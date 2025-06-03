#!/bin/bash
# NAT setup script for Quasar Relay testing
# Configures different NAT behaviors based on environment variables

# Set default NAT type if not specified
NAT_TYPE=${NAT_TYPE:-"FULL_CONE"}

# Configure sysctl settings for forwarding
echo 1 > /proc/sys/net/ipv4/ip_forward

echo "Starting NAT Simulator with NAT_TYPE: $NAT_TYPE"

# Configure iptables based on NAT type
case "$NAT_TYPE" in
    "FULL_CONE")
        echo "Configuring Full Cone NAT"
        # Full Cone NAT: Any external host can send packets to the internal client
        # Maps the internal client to a public IP/port combination and allows all inbound traffic to that mapping
        iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE
        iptables -A FORWARD -i eth0 -o eth1 -j ACCEPT
        iptables -A FORWARD -i eth1 -o eth0 -j ACCEPT
        ;;
        
    "ADDRESS_RESTRICTED")
        echo "Configuring Address Restricted NAT"
        # Address Restricted NAT: Only external hosts that the internal client has sent packets to can send packets back
        # Restricts inbound traffic based on source IP address
        iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE
        iptables -A FORWARD -i eth0 -o eth1 -m state --state RELATED,ESTABLISHED -j ACCEPT
        iptables -A FORWARD -i eth1 -o eth0 -j ACCEPT
        # Additional rules to enforce address restriction
        iptables -A INPUT -i eth0 -m state --state RELATED,ESTABLISHED -j ACCEPT
        iptables -A INPUT -i eth0 -m state --state NEW -j DROP
        ;;
        
    "PORT_RESTRICTED")
        echo "Configuring Port Restricted NAT"
        # Port Restricted NAT: Only external hosts with specific IP:port that the internal client has sent packets to can send packets back
        # Restricts inbound traffic based on source IP and port
        iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE
        iptables -A FORWARD -i eth0 -o eth1 -m state --state RELATED,ESTABLISHED -j ACCEPT
        iptables -A FORWARD -i eth1 -o eth0 -j ACCEPT
        # Additional rules to enforce port restriction
        iptables -A INPUT -i eth0 -m state --state RELATED,ESTABLISHED -j ACCEPT
        iptables -A INPUT -i eth0 -m state --state NEW -j DROP
        # Use connection tracking with stricter parameters
        echo 1 > /proc/sys/net/netfilter/nf_conntrack_tcp_strict
        ;;
        
    "SYMMETRIC")
        echo "Configuring Symmetric NAT"
        # Symmetric NAT: Creates a unique mapping for each internal client IP:port to destination IP:port combination
        # Most restrictive, creates new public port for each connection
        iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE --random
        iptables -A FORWARD -i eth0 -o eth1 -m state --state RELATED,ESTABLISHED -j ACCEPT
        iptables -A FORWARD -i eth1 -o eth0 -j ACCEPT
        # Force random port allocation
        echo 1 > /proc/sys/net/ipv4/ip_local_port_range
        echo 1024 65535 > /proc/sys/net/ipv4/ip_local_port_range
        # Set minimum time before port reuse
        echo 60 > /proc/sys/net/ipv4/tcp_fin_timeout
        ;;
        
    *)
        echo "Unknown NAT type: $NAT_TYPE. Using Full Cone NAT as default."
        iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE
        iptables -A FORWARD -i eth0 -o eth1 -j ACCEPT
        iptables -A FORWARD -i eth1 -o eth0 -j ACCEPT
        ;;
esac

# Display configured NAT settings
echo "NAT configuration complete. Current iptables rules:"
iptables -L -v
iptables -t nat -L -v

# Execute the provided command or keep the container running
if [ $# -eq 0 ]; then
    echo "No command provided, keeping container alive..."
    # Keep the container running
    tail -f /dev/null
else
    echo "Executing provided command: $@"
    exec "$@"
fi
