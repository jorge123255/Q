#!/bin/bash
# NAT Test Environment Deployment Script for Quasar Relay
# This script sets up Docker containers to simulate different NAT environments
# for testing the Quasar relay security implementation

# Color codes for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting Quasar Relay NAT Testing Environment Setup${NC}"
echo "==============================================="

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Error: Docker is not installed. Please install Docker first.${NC}"
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo -e "${YELLOW}Warning: Docker Compose not found. Some features may not be available.${NC}"
fi

# Create networks for simulating different environments
echo -e "\n${GREEN}Creating Docker networks for NAT simulation...${NC}"

# Public network (simulates the internet)
docker network create --subnet=172.20.0.0/16 quasar-relay-public || { 
    echo -e "${RED}Failed to create public network${NC}"; 
    exit 1; 
}

# Private networks (simulate different NAT environments)
docker network create --subnet=172.21.0.0/16 quasar-nat-fullcone || {
    echo -e "${RED}Failed to create full cone NAT network${NC}";
    exit 1;
}

docker network create --subnet=172.22.0.0/16 quasar-nat-restricted || {
    echo -e "${RED}Failed to create restricted NAT network${NC}";
    exit 1;
}

docker network create --subnet=172.23.0.0/16 quasar-nat-symmetric || {
    echo -e "${RED}Failed to create symmetric NAT network${NC}";
    exit 1;
}

echo -e "${GREEN}Networks created successfully${NC}"

# Create a directory for test artifacts if it doesn't exist
mkdir -p ./test-artifacts

# Deploy the relay server
echo -e "\n${GREEN}Deploying Quasar Relay server...${NC}"
docker run -d --name quasar-relay-server \
    --network quasar-relay-public \
    -p 8080:8080 \
    -p 3478:3478/udp \
    -v "$(pwd)/test-artifacts:/app/logs" \
    --env "RELAY_SERVER_URL=ws://0.0.0.0:8080" \
    --env "STUN_SERVER_PORT=3478" \
    --env "ENABLE_SECURITY=true" \
    --env "RATE_LIMIT=10" \
    --env "AUDIT_LOGGING=true" \
    quasar/relay-server:test || {
    echo -e "${RED}Failed to deploy relay server. Is the image 'quasar/relay-server:test' available?${NC}"
    echo -e "${YELLOW}You may need to build the test image first with:${NC}"
    echo "docker build -t quasar/relay-server:test -f Dockerfile.test ."
    exit 1;
}

# Set up NAT simulation containers
echo -e "\n${GREEN}Setting up NAT simulation containers...${NC}"

# Full Cone NAT simulation
echo "Deploying Full Cone NAT environment..."
docker run -d --name quasar-fullcone-nat \
    --cap-add=NET_ADMIN \
    --network quasar-nat-fullcone \
    --env "NAT_TYPE=FULL_CONE" \
    quasar/nat-simulator:test || {
    echo -e "${RED}Failed to deploy Full Cone NAT simulator${NC}";
}

# Connect the Full Cone NAT to the public network
docker network connect quasar-relay-public quasar-fullcone-nat

# Restricted NAT simulation
echo "Deploying Restricted NAT environment..."
docker run -d --name quasar-restricted-nat \
    --cap-add=NET_ADMIN \
    --network quasar-nat-restricted \
    --env "NAT_TYPE=PORT_RESTRICTED" \
    quasar/nat-simulator:test || {
    echo -e "${RED}Failed to deploy Restricted NAT simulator${NC}";
}

# Connect the Restricted NAT to the public network
docker network connect quasar-relay-public quasar-restricted-nat

# Symmetric NAT simulation
echo "Deploying Symmetric NAT environment..."
docker run -d --name quasar-symmetric-nat \
    --cap-add=NET_ADMIN \
    --network quasar-nat-symmetric \
    --env "NAT_TYPE=SYMMETRIC" \
    quasar/nat-simulator:test || {
    echo -e "${RED}Failed to deploy Symmetric NAT simulator${NC}";
}

# Connect the Symmetric NAT to the public network
docker network connect quasar-relay-public quasar-symmetric-nat

# Deploy test clients in each NAT environment
echo -e "\n${GREEN}Deploying test clients in NAT environments...${NC}"

# Client in Full Cone NAT
echo "Deploying client in Full Cone NAT..."
docker run -d --name quasar-client-fullcone \
    --network quasar-nat-fullcone \
    -v "$(pwd)/test-artifacts:/app/logs" \
    --env "RELAY_SERVER=quasar-relay-server:8080" \
    --env "TEST_MODE=true" \
    quasar/test-client:latest || {
    echo -e "${RED}Failed to deploy client in Full Cone NAT${NC}";
}

# Client in Restricted NAT
echo "Deploying client in Restricted NAT..."
docker run -d --name quasar-client-restricted \
    --network quasar-nat-restricted \
    -v "$(pwd)/test-artifacts:/app/logs" \
    --env "RELAY_SERVER=quasar-relay-server:8080" \
    --env "TEST_MODE=true" \
    quasar/test-client:latest || {
    echo -e "${RED}Failed to deploy client in Restricted NAT${NC}";
}

# Client in Symmetric NAT
echo "Deploying client in Symmetric NAT..."
docker run -d --name quasar-client-symmetric \
    --network quasar-nat-symmetric \
    -v "$(pwd)/test-artifacts:/app/logs" \
    --env "RELAY_SERVER=quasar-relay-server:8080" \
    --env "TEST_MODE=true" \
    quasar/test-client:latest || {
    echo -e "${RED}Failed to deploy client in Symmetric NAT${NC}";
}

# Configure NAT rules
echo -e "\n${GREEN}Configuring NAT simulation rules...${NC}"

# Full Cone NAT rules (most permissive)
docker exec quasar-fullcone-nat /bin/bash -c "
    iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE
    echo 1 > /proc/sys/net/ipv4/ip_forward
    echo 'Full Cone NAT rules configured'
"

# Port-Restricted NAT rules
docker exec quasar-restricted-nat /bin/bash -c "
    iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE
    iptables -A FORWARD -i eth0 -o eth1 -m state --state RELATED,ESTABLISHED -j ACCEPT
    iptables -A FORWARD -i eth1 -o eth0 -j ACCEPT
    echo 1 > /proc/sys/net/ipv4/ip_forward
    echo 'Port-Restricted NAT rules configured'
"

# Symmetric NAT rules (most restrictive)
docker exec quasar-symmetric-nat /bin/bash -c "
    iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE --random
    iptables -A FORWARD -i eth0 -o eth1 -m state --state RELATED,ESTABLISHED -j ACCEPT
    iptables -A FORWARD -i eth1 -o eth0 -j ACCEPT
    echo 1 > /proc/sys/net/ipv4/ip_forward
    echo 'Symmetric NAT rules configured'
"

echo -e "\n${GREEN}NAT testing environment setup complete!${NC}"
echo -e "\nTo run tests in this environment:"
echo -e "1. Execute: ${YELLOW}dotnet test Quasar.Relay.Tests --filter \"Category=NatSimulation\"${NC}"
echo -e "2. View logs in the ${YELLOW}./test-artifacts${NC} directory"
echo -e "\nTo tear down the environment when finished:"
echo -e "${YELLOW}./cleanup-nat-test-environment.sh${NC}"

# Create cleanup script
cat > ./cleanup-nat-test-environment.sh << 'EOF'
#!/bin/bash
# Cleanup script for NAT testing environment

echo "Cleaning up Quasar Relay NAT testing environment..."

# Stop and remove containers
echo "Stopping and removing containers..."
docker rm -f quasar-relay-server quasar-client-fullcone quasar-client-restricted quasar-client-symmetric quasar-fullcone-nat quasar-restricted-nat quasar-symmetric-nat

# Remove networks
echo "Removing Docker networks..."
docker network rm quasar-relay-public quasar-nat-fullcone quasar-nat-restricted quasar-nat-symmetric

echo "Cleanup complete!"
EOF

chmod +x ./cleanup-nat-test-environment.sh

echo -e "\n${GREEN}Setup complete. NAT testing environment is ready.${NC}"
