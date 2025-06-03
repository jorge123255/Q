# Quasar Relay Security Testing Guide

This guide provides instructions for testing the Quasar Relay security implementation across different NAT environments. These tests validate that our end-to-end encryption, rate limiting, secure credential storage, and audit logging function correctly in various network scenarios.

## Prerequisites

- Docker and Docker Compose installed
- .NET Core SDK 4.5.2 or higher
- Access to build the required Docker images

## Getting Started

1. Make the deployment script executable:
   ```bash
   chmod +x deploy-nat-test-environment.sh
   ```

2. Build the NAT simulator image:
   ```bash
   docker build -t quasar/nat-simulator:test -f Dockerfile.nat-simulator .
   ```

3. Build a test relay server image:
   ```bash
   docker build -t quasar/relay-server:test -f ../Dockerfile.relay --build-arg CONFIG=test .
   ```

4. Build the test client image:
   ```bash
   docker build -t quasar/test-client:latest -f ../Dockerfile --target test-client .
   ```

## Running the NAT Simulation Tests

### Method 1: Docker-based Testing Environment

1. Deploy the NAT testing environment:
   ```bash
   ./deploy-nat-test-environment.sh
   ```

2. Verify containers are running:
   ```bash
   docker ps
   ```

3. View logs from the relay server:
   ```bash
   docker logs quasar-relay-server
   ```

4. Run manual tests by executing commands in the client containers:
   ```bash
   docker exec -it quasar-client-fullcone /bin/bash
   # Then run test commands inside the container
   ```

5. When finished, clean up the environment:
   ```bash
   ./cleanup-nat-test-environment.sh
   ```

### Method 2: Unit Tests with Mocked NAT Behavior

1. Run the NAT simulation unit tests:
   ```bash
   dotnet test Quasar.Relay.Tests --filter "Category=NatSimulation"
   ```

2. View test results and analyze any failures.

## What We're Testing

Our tests validate the following aspects of the relay security implementation:

### 1. Connection Through Different NAT Types

- **Full Cone NAT**: Simplest case, should work with direct connections
- **Address-Restricted NAT**: May require STUN, but should work reliably
- **Port-Restricted NAT**: Requires proper STUN/TURN implementation
- **Symmetric NAT**: Most challenging, requires relay server

### 2. Security Features Under NAT

- **End-to-End Encryption**: Verifies that encryption works despite NAT traversal
- **Rate Limiting**: Confirms rate limiting works regardless of NAT type
- **Credential Storage**: Tests secure storage and retrieval in different network scenarios
- **Audit Logging**: Validates comprehensive event logging across NAT boundaries

## Expected Test Results

When tests pass successfully, you should see:

1. Successful connections across all NAT types
2. Proper encryption/decryption of messages
3. Rate limit enforcement when connection attempt thresholds are exceeded
4. Secure storage and retrieval of credentials
5. Comprehensive audit logs capturing connection attempts and security events

## Troubleshooting

### Common Issues

1. **Connection Failures in Symmetric NAT**:
   - Verify TURN server is properly configured
   - Check that relay server is accessible from the client
   - Confirm proper relay connection fallback

2. **Rate Limiting Not Working**:
   - Verify rate limit settings in the relay server
   - Check that client properly handles rate limit responses
   - Examine audit logs for rate limit events

3. **Encryption Issues**:
   - Verify key exchange is working properly
   - Check that encrypted content can be properly decrypted
   - Confirm password-based encryption is functioning

4. **Missing Audit Logs**:
   - Check audit logging is enabled
   - Verify log path permissions
   - Confirm events are being properly triggered

## Next Steps

After successfully completing the NAT simulation tests:

1. Update the implementation plan to mark NAT testing as complete
2. Proceed with beta deployment to real-world clients
3. Conduct load testing on the relay infrastructure
4. Perform a comprehensive security assessment

## Additional Resources

- [NAT Types and Connection Methods](https://tailscale.com/blog/how-nat-traversal-works/)
- [Testing NAT Traversal with Docker](https://www.docker.com/blog/testing-nat-traversal-with-docker/)
- [STUN/TURN Server Configuration Guide](https://webrtc.org/getting-started/turn-server)
