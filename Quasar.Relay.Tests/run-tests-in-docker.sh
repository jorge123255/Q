#!/bin/bash
# Run NAT simulation tests in a .NET Docker container

echo "===== Running Quasar Relay NAT Simulation Tests in Docker ====="

# Create a Docker network for the tests
docker network create quasar-test-network || echo "Network already exists"

# Run the tests in a .NET SDK container
docker run --rm \
  --name quasar-test-runner \
  --network quasar-test-network \
  -v "$(pwd)/..:/app" \
  -w /app \
  mcr.microsoft.com/dotnet/sdk:6.0 \
  bash -c "cd Quasar.Relay.Tests && dotnet test --filter \"Category=NatSimulation\" -v n"

# Capture the exit code
TEST_EXIT_CODE=$?

# Clean up
docker network rm quasar-test-network

echo "===== NAT Simulation Tests Completed with Exit Code: $TEST_EXIT_CODE ====="

exit $TEST_EXIT_CODE
