# Quasar Relay Load Testing Guide

This guide explains how to use the load testing tools to validate your relay server's performance, security features, and rate limiting in a production environment.

## Overview

The load testing tools simulate multiple clients connecting to your relay server simultaneously, testing:

1. Connection handling under high load
2. End-to-end encryption with password-derived keys
3. Rate limiting enforcement
4. Server stability and resource usage

## Prerequisites

- A deployed Quasar relay server (follow the DigitalOcean Quickstart Guide)
- .NET 6.0 SDK or later installed on your testing machine
- Access to monitor the server metrics (Grafana dashboard)
- Sufficient bandwidth on your testing machine

## Running Load Tests

### Method 1: Using Visual Studio

1. Open the Quasar solution in Visual Studio
2. Set the `Quasar.Relay.Tests.LoadTesting` project as the startup project
3. Run the project (F5)
4. Enter the URL and test parameters when prompted

### Method 2: Command Line

1. Navigate to the load testing directory:
   ```
   cd /Users/georgeszulc/Desktop/Projects\ 2024/Quasar-master/Quasar.Relay.Tests/LoadTesting
   ```

2. Build the project:
   ```
   dotnet build
   ```

3. Run the load test:
   ```
   dotnet run -- wss://your-domain.com 100 10 true YourPassword 60
   ```

   Parameters:
   - Relay URL (required): `wss://your-domain.com`
   - Max connections: 100 (default)
   - Connection rate: 10 per second (default)
   - Use encryption: true (default)
   - Password: YourPassword (default: LoadTestPassword123!)
   - Test duration: 60 seconds (default)

## Recommended Test Scenarios

### 1. Basic Connectivity Test

```
dotnet run -- wss://your-domain.com 20 5 true YourPassword 30
```

This establishes 20 connections at a rate of 5 per second for 30 seconds, verifying basic connectivity and encryption.

### 2. Rate Limiting Test

```
dotnet run -- wss://your-domain.com 50 30 true YourPassword 60
```

This attempts to establish connections at a rate higher than your configured rate limit (default: 20 per minute). You should see rate limiting errors in the output and server logs.

### 3. High Volume Test

```
dotnet run -- wss://your-domain.com 200 10 true YourPassword 120
```

This tests how your server handles a higher number of concurrent connections over a longer period.

### 4. Encryption Validation

Run two tests - one with encryption and one without:

```
dotnet run -- wss://your-domain.com 50 10 true YourPassword 60
dotnet run -- wss://your-domain.com 50 10 false "" 60
```

If you've configured `ENCRYPTION_REQUIRED=true` on your server, the second test should fail with encryption-related errors.

## Interpreting Results

The load test will output:

- **Connection Statistics**: Success rates, failures, and rate-limited connections
- **Message Throughput**: Messages per second sent and received
- **Error Distribution**: Types of errors encountered (rate limiting, encryption, etc.)

### Monitor Your Server During Tests

While the load test is running:

1. Monitor the Grafana dashboard at `https://monitoring.your-domain.com`
2. Pay attention to:
   - CPU and memory usage
   - Connection counts
   - Rate limiting events
   - Security events
   - Message throughput

## Adjusting Server Configuration

Based on test results, you may want to adjust your server configuration in `config/relay/relay-config.json`:

- Increase `maxConnections` if your server can handle more concurrent connections
- Adjust rate limiting parameters if they're too strict or too lenient
- Tune `connectionTimeout` based on your usage patterns

Remember to restart your relay server after changing configurations:

```bash
docker-compose -f docker-compose.production.yml restart quasar-relay-1 quasar-relay-2
```

## Troubleshooting

### Connection Failures

- Verify your firewall allows WebSocket connections on port 443
- Check SSL certificate configuration
- Ensure NGINX is properly configured for WebSocket proxying

### Rate Limiting Issues

- Review rate limiting configuration in `relay-config.json`
- Check logs for rate limiting events
- Verify the IP detection is working correctly behind the load balancer

### Encryption Problems

- Ensure clients and server are using compatible encryption settings
- Verify password-derived keys are being generated correctly
- Check audit logs for encryption/decryption failures

## Next Steps

After validating your relay server's performance and security:

1. Update your implementation plan to mark load testing as completed
2. Make any necessary configuration adjustments based on test results
3. Document your production server's performance characteristics
4. Proceed with full production deployment if all tests pass
