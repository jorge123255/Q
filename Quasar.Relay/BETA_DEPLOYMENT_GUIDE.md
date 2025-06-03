# Quasar Relay Beta Deployment Guide

This guide outlines the steps for deploying the Quasar relay system for beta testing with limited users.

## Prerequisites

- Docker and Docker Compose installed on the host server
- Domain name configured for the relay server
- SSL certificates (Let's Encrypt recommended)
- Access to cloud infrastructure (AWS, Azure, or Digital Ocean)
- Firewall configured to allow required ports

## Server Requirements

- **CPU**: 2+ cores recommended
- **RAM**: 4GB minimum
- **Storage**: 20GB minimum
- **Network**: Static IP address
- **Bandwidth**: 100Mbps+ recommended for multiple concurrent connections

## Deployment Steps

### 1. Server Preparation

```bash
# Update server packages
apt-get update && apt-get upgrade -y

# Install Docker and Docker Compose
apt-get install -y docker.io docker-compose

# Create deployment directory
mkdir -p /opt/quasar-relay
cd /opt/quasar-relay
```

### 2. Configuration Setup

Create the following configuration files:

**docker-compose.yml**
```yaml
version: '3'

services:
  relay-server:
    image: quasar/relay-server:beta
    container_name: quasar-relay
    ports:
      - "443:8080"
      - "3478:3478/udp"
    volumes:
      - ./config:/app/config
      - ./logs:/app/logs
      - ./certs:/app/certs
    environment:
      - RELAY_SERVER_URL=wss://your-domain.com
      - STUN_SERVER_PORT=3478
      - ENABLE_SECURITY=true
      - RATE_LIMIT=10
      - AUDIT_LOGGING=true
    restart: unless-stopped

  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus:/etc/prometheus
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
    restart: unless-stopped

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    ports:
      - "3000:3000"
    volumes:
      - grafana_data:/var/lib/grafana
    depends_on:
      - prometheus
    restart: unless-stopped

volumes:
  prometheus_data:
  grafana_data:
```

**config/relay-config.json**
```json
{
  "server": {
    "host": "0.0.0.0",
    "port": 8080,
    "ssl": {
      "enabled": true,
      "certPath": "/app/certs/fullchain.pem",
      "keyPath": "/app/certs/privkey.pem"
    }
  },
  "security": {
    "rateLimit": {
      "enabled": true,
      "maxConnections": 10,
      "timeWindowMs": 60000
    },
    "encryption": {
      "enforced": true,
      "keyDerivationIterations": 10000
    },
    "auditLogging": {
      "enabled": true,
      "logPath": "/app/logs/audit.log",
      "rotationSize": "10MB",
      "maxFiles": 10
    }
  },
  "stun": {
    "enabled": true,
    "port": 3478
  },
  "monitoring": {
    "metrics": {
      "enabled": true,
      "endpoint": "/metrics"
    },
    "healthCheck": {
      "enabled": true,
      "endpoint": "/health"
    }
  }
}
```

**prometheus/prometheus.yml**
```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'quasar-relay'
    static_configs:
      - targets: ['relay-server:8080']
    metrics_path: /metrics
```

### 3. SSL Certificate Setup

```bash
# Create certs directory
mkdir -p certs

# Option 1: Use Let's Encrypt (recommended for production)
certbot certonly --standalone -d your-domain.com
cp /etc/letsencrypt/live/your-domain.com/fullchain.pem certs/
cp /etc/letsencrypt/live/your-domain.com/privkey.pem certs/

# Option 2: Generate self-signed certificate (for testing only)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout certs/privkey.pem -out certs/fullchain.pem
```

### 4. Deployment

```bash
# Build and deploy containers
docker-compose up -d

# Verify containers are running
docker-compose ps

# Check logs
docker-compose logs -f relay-server
```

### 5. Monitoring Setup

1. Access Grafana at http://your-server-ip:3000 (default credentials: admin/admin)
2. Add Prometheus as a data source (URL: http://prometheus:9090)
3. Import the provided Quasar Relay dashboard (ID: 15000)

### 6. Beta Tester Onboarding

1. Provide beta testers with:
   - Latest Quasar client build with relay support
   - Relay server address (wss://your-domain.com)
   - Test credentials if applicable
   - Feedback submission form/process

2. Configure beta client limits:
   - Edit `config/beta-users.json` to add authorized beta tester IDs
   - Set appropriate rate limits for beta testing

### 7. Testing Verification

Perform these checks after deployment:

1. Verify SSL/TLS configuration:
   ```bash
   openssl s_client -connect your-domain.com:443 -servername your-domain.com
   ```

2. Test STUN server:
   ```bash
   # Install STUN client
   apt-get install -y stuntman-client
   
   # Test STUN connectivity
   stunclient your-domain.com 3478
   ```

3. Monitor connection metrics in Grafana dashboard

## Security Considerations

1. **Firewall Configuration**:
   - Allow TCP port 443 (WebSocket/HTTPS)
   - Allow UDP port 3478 (STUN)
   - Restrict access to ports 9090 and 3000 to admin IPs only

2. **Rate Limiting**:
   - Default settings (10 connections per minute) should be sufficient for beta
   - Monitor and adjust based on usage patterns

3. **Log Monitoring**:
   - Set up log rotation for audit logs
   - Implement log monitoring for security events
   - Consider log forwarding to a central SIEM if available

4. **Access Control**:
   - Use strong passwords for admin interfaces
   - Consider implementing IP restrictions for management endpoints

## Rollback Plan

In case of critical issues:

1. Restore previous version:
   ```bash
   docker-compose down
   docker-compose -f docker-compose.previous.yml up -d
   ```

2. If data corruption is suspected:
   ```bash
   docker-compose down
   cp -r backup/data/* data/
   docker-compose up -d
   ```

## Beta Feedback Collection

Create a feedback collection process for beta testers:

1. Implement an in-app feedback mechanism
2. Set up a dedicated email address for bug reports
3. Create a private forum or Discord channel for beta testers
4. Schedule regular check-ins with key beta testers

## Next Steps After Beta

1. Analyze beta testing results and feedback
2. Address critical issues before proceeding to production
3. Conduct load testing with simulated users
4. Perform final security assessment
5. Prepare production deployment documentation
6. Create user guides and administrative documentation
