# Quasar Relay Production Deployment Guide

This guide outlines the steps for deploying the Quasar relay system in a production environment.

## Prerequisites

- Docker and Docker Compose installed on the host server
- Domain name with DNS configured for the relay server
- SSL certificates (Let's Encrypt recommended)
- Cloud infrastructure (AWS, Azure, or Digital Ocean)
- Firewall and network security groups configured

## Server Requirements

- **CPU**: 4+ cores recommended for production
- **RAM**: 8GB minimum
- **Storage**: 50GB SSD minimum
- **Network**: Static IP address
- **Bandwidth**: 1Gbps recommended for multiple concurrent connections

## Production Deployment Architecture

The production deployment consists of:

1. **Relay Servers**: Multiple relay server instances behind a load balancer
2. **Database**: For credential storage and session management
3. **Monitoring Stack**: Prometheus and Grafana for metrics and alerts
4. **Logging**: Centralized logging with ELK or similar
5. **Load Balancer**: For distributing traffic across relay instances
6. **Backup System**: For regular data backups

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

# Create necessary subdirectories
mkdir -p config logs certs data backups monitoring
```

### 2. Docker Compose Configuration

Create a `docker-compose.yml` file with production settings:

```yaml
version: '3.8'

services:
  relay-load-balancer:
    image: nginx:latest
    container_name: relay-load-balancer
    ports:
      - "443:443"
    volumes:
      - ./config/nginx:/etc/nginx/conf.d
      - ./certs:/etc/nginx/certs
    depends_on:
      - relay-server-1
      - relay-server-2
    restart: unless-stopped
    logging:
      driver: "json-file"
      options:
        max-size: "100m"
        max-file: "3"

  relay-server-1:
    image: quasar/relay-server:production
    container_name: quasar-relay-1
    expose:
      - "8080"
    ports:
      - "3478:3478/udp"
    volumes:
      - ./config/relay:/app/config
      - ./logs/relay1:/app/logs
      - ./data/relay1:/app/data
    environment:
      - NODE_ENV=production
      - RELAY_SERVER_URL=wss://your-domain.com
      - RELAY_SERVER_ID=relay1
      - STUN_SERVER_PORT=3478
      - ENABLE_SECURITY=true
      - RATE_LIMIT=20
      - RATE_LIMIT_WINDOW=60000
      - AUDIT_LOGGING=true
      - ENCRYPTION_REQUIRED=true
    restart: unless-stopped
    logging:
      driver: "json-file"
      options:
        max-size: "200m"
        max-file: "5"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  relay-server-2:
    image: quasar/relay-server:production
    container_name: quasar-relay-2
    expose:
      - "8080"
    ports:
      - "3479:3478/udp"
    volumes:
      - ./config/relay:/app/config
      - ./logs/relay2:/app/logs
      - ./data/relay2:/app/data
    environment:
      - NODE_ENV=production
      - RELAY_SERVER_URL=wss://your-domain.com
      - RELAY_SERVER_ID=relay2
      - STUN_SERVER_PORT=3478
      - ENABLE_SECURITY=true
      - RATE_LIMIT=20
      - RATE_LIMIT_WINDOW=60000
      - AUDIT_LOGGING=true
      - ENCRYPTION_REQUIRED=true
    restart: unless-stopped
    logging:
      driver: "json-file"
      options:
        max-size: "200m"
        max-file: "5"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    volumes:
      - ./config/prometheus:/etc/prometheus
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.retention.time=30d'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
    restart: unless-stopped
    expose:
      - "9090"
    logging:
      driver: "json-file"
      options:
        max-size: "100m"
        max-file: "3"
    user: "65534:65534"

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    volumes:
      - grafana_data:/var/lib/grafana
      - ./config/grafana/provisioning:/etc/grafana/provisioning
    depends_on:
      - prometheus
    restart: unless-stopped
    expose:
      - "3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_ADMIN_PASSWORD:-admin}
      - GF_USERS_ALLOW_SIGN_UP=false
      - GF_SERVER_ROOT_URL=https://monitoring.your-domain.com
    logging:
      driver: "json-file"
      options:
        max-size: "100m"
        max-file: "3"

  node-exporter:
    image: prom/node-exporter:latest
    container_name: node-exporter
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    command:
      - '--path.procfs=/host/proc'
      - '--path.sysfs=/host/sys'
      - '--collector.filesystem.ignored-mount-points=^/(sys|proc|dev|host|etc)($$|/)'
    restart: unless-stopped
    expose:
      - "9100"
    logging:
      driver: "json-file"
      options:
        max-size: "50m"
        max-file: "3"

  cadvisor:
    image: gcr.io/cadvisor/cadvisor:latest
    container_name: cadvisor
    volumes:
      - /:/rootfs:ro
      - /var/run:/var/run:ro
      - /sys:/sys:ro
      - /var/lib/docker/:/var/lib/docker:ro
      - /dev/disk/:/dev/disk:ro
    restart: unless-stopped
    expose:
      - "8080"
    logging:
      driver: "json-file"
      options:
        max-size: "50m"
        max-file: "3"

  backup-service:
    image: debian:bullseye-slim
    container_name: backup-service
    volumes:
      - ./data:/data:ro
      - ./logs:/logs:ro
      - ./backups:/backups
      - ./config/backup:/etc/backup
      - ./config/backup/backup.sh:/usr/local/bin/backup.sh
    entrypoint: ["/bin/bash", "-c"]
    command: |
      chmod +x /usr/local/bin/backup.sh && 
      echo "0 2 * * * /usr/local/bin/backup.sh >> /backups/backup.log 2>&1" > /etc/crontab && 
      cron -f
    restart: unless-stopped
    logging:
      driver: "json-file"
      options:
        max-size: "50m"
        max-file: "3"

  security-gateway:
    image: nginx:latest
    container_name: security-gateway
    ports:
      - "80:80"
    volumes:
      - ./config/nginx/security.conf:/etc/nginx/conf.d/default.conf
    restart: unless-stopped
    logging:
      driver: "json-file"
      options:
        max-size: "50m"
        max-file: "3"

volumes:
  prometheus_data:
  grafana_data:
```

### 3. Configure NGINX Load Balancer

Create a load balancer configuration in `config/nginx/relay.conf`:

```nginx
upstream relay_backend {
    # IP hash ensures clients connect to the same backend server
    ip_hash;
    server relay-server-1:8080;
    server relay-server-2:8080;
    # Add more relay servers as needed
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /etc/nginx/certs/fullchain.pem;
    ssl_certificate_key /etc/nginx/certs/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options nosniff;
    add_header X-Frame-Options DENY;
    add_header X-XSS-Protection "1; mode=block";

    # WebSocket proxy
    location / {
        proxy_pass http://relay_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # WebSocket timeout settings
        proxy_read_timeout 3600s;
        proxy_send_timeout 3600s;

        # Connection limiting
        limit_conn conn_limit_per_ip 20;
        limit_req zone=req_limit_per_ip burst=20 nodelay;
    }

    # Health check endpoint
    location /health {
        proxy_pass http://relay_backend/health;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        
        # Only allow internal access
        allow 127.0.0.1;
        allow 10.0.0.0/8;
        allow 172.16.0.0/12;
        allow 192.168.0.0/16;
        deny all;
    }

    # Metrics endpoint for Prometheus
    location /metrics {
        proxy_pass http://relay_backend/metrics;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        
        # Only allow internal access
        allow 127.0.0.1;
        allow 10.0.0.0/8;
        allow 172.16.0.0/12;
        allow 192.168.0.0/16;
        deny all;
    }
}

# Monitoring subdomains
server {
    listen 443 ssl http2;
    server_name monitoring.your-domain.com;

    ssl_certificate /etc/nginx/certs/fullchain.pem;
    ssl_certificate_key /etc/nginx/certs/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;

    # Strong authentication for monitoring
    auth_basic "Restricted Access";
    auth_basic_user_file /etc/nginx/conf.d/.htpasswd;

    location / {
        proxy_pass http://grafana:3000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}

# HTTP to HTTPS redirect
server {
    listen 80;
    server_name your-domain.com monitoring.your-domain.com;
    return 301 https://$host$request_uri;
}
```

### 4. Configure Prometheus

Create `config/prometheus/prometheus.yml`:

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  scrape_timeout: 10s

alerting:
  alertmanagers:
    - static_configs:
        - targets: ['alertmanager:9093']

rule_files:
  - "alert_rules.yml"

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'relay-servers'
    metrics_path: /metrics
    static_configs:
      - targets: ['relay-server-1:8080', 'relay-server-2:8080']
        labels:
          service: 'quasar-relay'

  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']

  - job_name: 'cadvisor'
    static_configs:
      - targets: ['cadvisor:8080']
```

Create `config/prometheus/alert_rules.yml`:

```yaml
groups:
  - name: relay_alerts
    rules:
      - alert: RelayServerDown
        expr: up{job="relay-servers"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Relay Server Down"
          description: "Relay server {{ $labels.instance }} has been down for more than 1 minute."

      - alert: HighConnectionRate
        expr: rate(quasar_relay_connection_attempts_total[5m]) > 50
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High Connection Rate"
          description: "Relay server {{ $labels.instance }} has high connection rate ({{ $value }} connections/s)."

      - alert: HighSecurityEventRate
        expr: rate(quasar_relay_security_events_total[5m]) > 10
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High Security Event Rate"
          description: "Relay server {{ $labels.instance }} has high security event rate ({{ $value }} events/s)."

      - alert: HighMemoryUsage
        expr: (node_memory_MemTotal_bytes - node_memory_MemAvailable_bytes) / node_memory_MemTotal_bytes * 100 > 85
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High Memory Usage"
          description: "Server memory usage is above 85% for more than 5 minutes."

      - alert: HighCPUUsage
        expr: 100 - (avg by(instance) (irate(node_cpu_seconds_total{mode="idle"}[5m])) * 100) > 80
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High CPU Usage"
          description: "Server CPU usage is above 80% for more than 5 minutes."
```

### 5. Create Backup Script

Create `config/backup/backup.sh`:

```bash
#!/bin/bash
# Backup script for Quasar Relay production environment

# Set variables
BACKUP_DIR="/backups"
DATA_DIR="/data"
LOGS_DIR="/logs"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_NAME="quasar_relay_backup_${TIMESTAMP}"

# Create backup directory with timestamp
mkdir -p "${BACKUP_DIR}/${BACKUP_NAME}"

# Backup data
echo "Backing up data at $(date)"
tar -czf "${BACKUP_DIR}/${BACKUP_NAME}/data.tar.gz" -C "${DATA_DIR}" .

# Backup logs
echo "Backing up logs at $(date)"
tar -czf "${BACKUP_DIR}/${BACKUP_NAME}/logs.tar.gz" -C "${LOGS_DIR}" .

# Cleanup old backups (keep last 7 days)
find "${BACKUP_DIR}" -type d -name "quasar_relay_backup_*" -mtime +7 -exec rm -rf {} \;

echo "Backup completed at $(date)"
echo "Backup stored at ${BACKUP_DIR}/${BACKUP_NAME}"

# Optional: Send backup to remote storage
# aws s3 cp "${BACKUP_DIR}/${BACKUP_NAME}" s3://your-backup-bucket/quasar-relay/ --recursive
```

### 6. Security Configuration for Relay Server

Create `config/relay/relay-config.json`:

```json
{
  "server": {
    "host": "0.0.0.0",
    "port": 8080,
    "maxConnections": 2000,
    "connectionTimeout": 300000
  },
  "security": {
    "rateLimit": {
      "enabled": true,
      "maxConnections": 20,
      "timeWindowMs": 60000,
      "banDuration": 3600000
    },
    "encryption": {
      "enforced": true,
      "keyDerivationIterations": 10000,
      "keyLength": 256,
      "algorithm": "aes-256-gcm"
    },
    "auditLogging": {
      "enabled": true,
      "logPath": "/app/logs/audit.log",
      "rotationSize": "100MB",
      "maxFiles": 30,
      "logSecurityEvents": true,
      "logConnectionEvents": true,
      "logDataEvents": false
    },
    "ipBlacklist": {
      "enabled": true,
      "maxFailedAttempts": 10,
      "resetTimeWindow": 86400000,
      "banDuration": 86400000
    }
  },
  "stun": {
    "enabled": true,
    "port": 3478,
    "publicIP": "auto"
  },
  "monitoring": {
    "metrics": {
      "enabled": true,
      "endpoint": "/metrics",
      "collectSystemMetrics": true,
      "collectConnectionMetrics": true,
      "collectSecurityMetrics": true
    },
    "healthCheck": {
      "enabled": true,
      "endpoint": "/health",
      "includeDetails": false
    }
  },
  "clustering": {
    "enabled": true,
    "syncInterval": 5000
  }
}
```

### 7. SSL Certificate Setup

```bash
# Create certs directory
mkdir -p certs

# Option 1: Use Let's Encrypt (recommended for production)
certbot certonly --standalone -d your-domain.com -d monitoring.your-domain.com
cp /etc/letsencrypt/live/your-domain.com/fullchain.pem certs/
cp /etc/letsencrypt/live/your-domain.com/privkey.pem certs/

# Create auto-renewal script
cat > /etc/cron.d/certbot-renewal << EOF
0 0,12 * * * root certbot renew --quiet --post-hook "docker-compose -f /opt/quasar-relay/docker-compose.yml restart relay-load-balancer"
EOF
```

### 8. Create Authentication for Monitoring

```bash
# Install htpasswd utility
apt-get install -y apache2-utils

# Create authentication file
htpasswd -c /opt/quasar-relay/config/nginx/.htpasswd admin
```

### 9. Deployment

```bash
# Build the Quasar relay server Docker image
cd /path/to/quasar/source
docker build -t quasar/relay-server:production -f Dockerfile.relay .

# Deploy containers
cd /opt/quasar-relay
docker-compose up -d

# Verify containers are running
docker-compose ps

# Check logs
docker-compose logs -f relay-load-balancer relay-server-1 relay-server-2
```

### 10. Monitoring and Alerting Setup

Import the Grafana dashboard we created:

1. Access Grafana at https://monitoring.your-domain.com
2. Log in with the admin credentials
3. Go to Dashboards > Import
4. Upload the dashboard JSON file from `/path/to/quasar/source/Quasar.Relay/monitoring/dashboard.json`

Configure alerting in Grafana:

1. Go to Alerting > Notification channels
2. Add email and/or Slack notification channels
3. Configure alert rules to use these channels

### 11. Production Verification Tests

After deployment, perform these tests to verify the production setup:

1. **Connection Tests**:
   ```bash
   # Test SSL configuration
   openssl s_client -connect your-domain.com:443 -servername your-domain.com
   
   # Test WebSocket connectivity
   wscat -c wss://your-domain.com
   ```

2. **Security Tests**:
   ```bash
   # SSL security check
   ssllabs-scan your-domain.com
   
   # Security headers check
   curl -I https://your-domain.com
   ```

3. **Load Tests**:
   ```bash
   # Install hey load testing tool
   go get -u github.com/rakyll/hey
   
   # Run load test
   hey -n 1000 -c 100 https://your-domain.com
   ```

### 12. Disaster Recovery Procedure

In case of service disruption:

1. **Single Relay Server Failure**:
   - Load balancer will automatically route traffic to healthy servers
   - Check server logs: `docker-compose logs relay-server-1`
   - Restart failed container: `docker-compose restart relay-server-1`

2. **Complete Service Failure**:
   - Stop all services: `docker-compose down`
   - Verify configuration files
   - Restart services: `docker-compose up -d`
   - Monitor logs: `docker-compose logs -f`

3. **Data Corruption**:
   ```bash
   # Stop services
   docker-compose down
   
   # Restore from backup
   BACKUP_DATE="20250601_120000"  # Replace with actual backup date
   tar -xzf /opt/quasar-relay/backups/quasar_relay_backup_${BACKUP_DATE}/data.tar.gz -C /opt/quasar-relay/data
   
   # Restart services
   docker-compose up -d
   ```

## Security Considerations

1. **Network Security**:
   - Configure firewall to allow only necessary ports:
     - 80/443 for HTTP/HTTPS
     - 3478/UDP for STUN
   - Use security groups to restrict access to management interfaces

2. **Authentication**:
   - Use strong passwords for all administrative interfaces
   - Consider implementing 2FA for admin access
   - Rotate credentials regularly

3. **Encryption**:
   - Enforce end-to-end encryption for all relay connections
   - Use strong TLS configuration for the web server
   - Regularly update SSL certificates

4. **Monitoring and Alerting**:
   - Set up alerts for security events
   - Monitor connection attempts and bandwidth usage
   - Set up log analysis to detect unusual patterns

5. **Updates and Maintenance**:
   - Establish a regular update schedule for all components
   - Test updates in a staging environment before production
   - Maintain a change log for all system modifications

## Performance Tuning

For high-traffic deployments, consider these performance optimizations:

1. **System Tuning**:
   ```bash
   # Increase maximum file descriptors
   echo "fs.file-max = 2097152" >> /etc/sysctl.conf
   
   # Increase network buffer sizes
   echo "net.core.rmem_max = 16777216" >> /etc/sysctl.conf
   echo "net.core.wmem_max = 16777216" >> /etc/sysctl.conf
   
   # Apply changes
   sysctl -p
   ```

2. **NGINX Tuning**:
   - Increase worker connections and processes
   - Optimize SSL settings with session caching
   - Use HTTP/2 for improved performance

3. **Docker Tuning**:
   - Use dedicated storage driver (overlay2)
   - Allocate sufficient CPU and memory limits
   - Use host networking mode for high-throughput servers

## Maintenance Procedures

### Regular Maintenance

1. **Log Rotation**:
   - Logs are automatically rotated based on configuration
   - Archive old logs periodically

2. **Database Maintenance**:
   - Perform regular backups
   - Check database size and performance

3. **Security Updates**:
   - Apply OS security patches monthly
   - Update Docker images regularly

### Scaling Procedures

To scale the relay infrastructure:

1. **Horizontal Scaling**:
   - Add more relay server instances to the docker-compose.yml
   - Update the NGINX configuration to include new servers

2. **Vertical Scaling**:
   - Increase CPU/RAM allocation for existing containers
   - Optimize configuration parameters for higher load

3. **Geographic Distribution**:
   - Deploy relay servers in multiple regions
   - Implement geographic load balancing

## Documentation and Support

Maintain these documents for operational support:

1. **System Architecture Diagram**
2. **Network Configuration Details**
3. **Backup and Restore Procedures**
4. **Incident Response Plan**
5. **Contact Information for Support Team**

---

This production deployment guide provides a comprehensive approach to deploying, securing, and maintaining the Quasar relay system in a production environment. Follow these instructions carefully to ensure a robust, secure, and scalable deployment.
