# Quasar Relay NAT Testing Plan

This document outlines the comprehensive testing approach for the Quasar relay security implementation across different NAT (Network Address Translation) environments. It provides a structured methodology to validate that our end-to-end encryption, rate limiting, secure credential storage, and audit logging work correctly across various network configurations.

## 1. Testing Objectives

- Validate that relay connections work through different NAT types
- Verify that security measures function correctly in challenging network environments
- Ensure credentials are stored and retrieved securely
- Confirm rate limiting prevents abuse even in complex NAT scenarios
- Verify that audit logging captures all relevant security events

## 2. NAT Environment Simulation

### 2.1 NAT Types to Test

We will test the following NAT configurations:

1. **Full Cone NAT**: Most permissive; any external host can send packets to the internal client.
2. **Address-Restricted NAT**: Only allows packets from external hosts that the internal client has previously communicated with.
3. **Port-Restricted NAT**: Only allows packets from specific (host, port) pairs that the internal client has previously communicated with.
4. **Symmetric NAT**: Most restrictive; maps each client/destination pair to a unique external port. Challenging for many connection techniques.

### 2.2 Simulation Methods

#### 2.2.1 Local Docker Network Simulation

- Use Docker to create isolated network environments
- Configure iptables rules to simulate different NAT behaviors
- Deploy relay server and test clients in separate containers

**Setup Commands:**
```bash
# Create Docker networks
docker network create --subnet=172.18.0.0/16 relay-net-public
docker network create --subnet=172.19.0.0/16 relay-net-private

# Deploy relay server
docker run -d --name relay-server --network relay-net-public -p 8080:8080 quasar-relay

# Deploy test clients behind simulated NAT
docker run -d --name client1 --network relay-net-private quasar-test-client
```

#### 2.2.2 Virtual Machine Environment

- Use VirtualBox/VMware to create multiple networked VMs
- Configure one VM as the relay server
- Configure client VMs with different NAT configurations
- Test connections between clients across NAT boundaries

## 3. Test Scenarios

### 3.1 Basic Connectivity Tests

| Test ID | Description | Expected Result |
|---------|-------------|-----------------|
| CONN-01 | Connect client through Full Cone NAT | Connection succeeds |
| CONN-02 | Connect client through Address-Restricted NAT | Connection succeeds |
| CONN-03 | Connect client through Port-Restricted NAT | Connection succeeds |
| CONN-04 | Connect client through Symmetric NAT | Connection succeeds with relay |
| CONN-05 | Connect multiple clients behind same NAT | All connections succeed |

### 3.2 Security Feature Tests

| Test ID | Description | Expected Result |
|---------|-------------|-----------------|
| SEC-01 | Encrypted connection through NAT | Data properly encrypted/decrypted |
| SEC-02 | Rate limiting under NAT | Excessive connections blocked |
| SEC-03 | Credential storage and retrieval | Credentials securely stored and retrieved |
| SEC-04 | Connection auditing across NAT boundaries | All events properly logged |
| SEC-05 | Connection with invalid credentials | Connection refused with proper error |

### 3.3 Performance Tests

| Test ID | Description | Expected Result |
|---------|-------------|-----------------|
| PERF-01 | Latency measurement across NAT types | Acceptable latency (<200ms) |
| PERF-02 | Throughput measurement | Minimum 5 Mbps throughput |
| PERF-03 | Simultaneous connections | System handles 50+ concurrent connections |

## 4. Implementation Details

### 4.1 Test Framework

Our test implementation uses:
- MSTest for unit and integration tests
- Docker for network isolation
- Mock implementations of NAT environments
- Instrumented relay servers to capture metrics

### 4.2 Test Automation

Tests will be automated using:
- CI/CD pipeline integration
- PowerShell/Bash scripts for environment setup
- Centralized result collection and reporting

## 5. Test Execution Plan

### 5.1 Prerequisites

- Docker installed and configured
- .NET testing framework installed
- Network permissions to create virtual interfaces
- Required test credentials pre-configured

### 5.2 Execution Steps

1. Run local unit tests
   ```
   dotnet test Quasar.Relay.Tests
   ```

2. Deploy Docker-based NAT simulation environment
   ```
   ./deploy-nat-test-environment.sh
   ```

3. Execute integration tests against simulated environment
   ```
   dotnet test Quasar.Relay.Tests --filter "Category=NatSimulation"
   ```

4. Collect and analyze results
   ```
   ./collect-test-results.sh
   ```

## 6. Success Criteria

The relay security implementation will be considered successfully validated when:

1. All test cases pass across all NAT types
2. End-to-end encryption works correctly in all scenarios
3. Rate limiting effectively prevents abuse
4. Credentials are securely stored and cannot be compromised
5. Audit logging captures all relevant security events
6. Performance metrics meet or exceed the requirements

## 7. Known Limitations

- Symmetric NAT may require additional configuration for optimal performance
- Extremely restrictive firewalls may require additional ports to be opened
- Very high-latency connections (>500ms) may experience timeouts

## 8. Next Steps

After local NAT testing is complete:
1. Proceed to beta deployment with limited users
2. Conduct load testing on the relay infrastructure
3. Perform a comprehensive security assessment
4. Finalize deployment documentation
