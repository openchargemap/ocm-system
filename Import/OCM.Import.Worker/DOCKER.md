# OCM Import Worker - Docker Guide

This guide explains how to build and run the OCM Import Worker using Docker.

## Prerequisites

- Docker Desktop 4.0+ or Docker Engine 20.10+
- Docker Compose 2.0+ (optional, for docker-compose.yml)
- At least 4GB RAM allocated to Docker
- 10GB free disk space

## Quick Start

### Build the Image

From the repository root directory (`ocm-system`):

```bash
# Build using Docker
docker build -f Import/OCM.Import.Worker/Dockerfile -t ocm-import-worker:latest .

# Or build using Docker Compose
cd Import/OCM.Import.Worker
docker-compose build
```

### Run the Container

```bash
# Run with Docker
docker run -d \
  --name ocm-import-worker \
  --restart unless-stopped \
  -e DOTNET_ENVIRONMENT=Production \
  -v ocm-import-temp:/app/temp \
  -v ocm-import-logs:/app/logs \
  ocm-import-worker:latest

# Or run with Docker Compose
docker-compose up -d
```

## Configuration

### Environment Variables

Set these environment variables when running the container:

```bash
-e DOTNET_ENVIRONMENT=Production
-e ImportSettings__MasterAPIBaseUrl=https://api.openchargemap.io/v3
-e ImportSettings__TempFolderPath=/app/temp
-e ImportSettings__GeolocationShapefilePath=/app/Shapefiles/World
-e ImportSettings__ImportUserAgent=OCM.Import.Worker/1.0
```

### Using Azure Key Vault (Production)

```bash
-e KeyVaultName=your-keyvault-name
-e AZURE_CLIENT_ID=your-client-id
-e AZURE_CLIENT_SECRET=your-client-secret
-e AZURE_TENANT_ID=your-tenant-id
```

### Using appsettings.json

Mount a custom configuration file:

```bash
-v /path/to/your/appsettings.Production.json:/app/appsettings.Production.json
```

## Docker Commands

### Build Commands

```bash
# Build with specific configuration
docker build \
  -f Import/OCM.Import.Worker/Dockerfile \
  --build-arg BUILD_CONFIGURATION=Release \
  -t ocm-import-worker:v1.0.0 \
  .

# Build with no cache (clean build)
docker build --no-cache \
  -f Import/OCM.Import.Worker/Dockerfile \
  -t ocm-import-worker:latest \
  .
```

### Run Commands

```bash
# Run interactively (see logs in console)
docker run -it --rm \
  -e DOTNET_ENVIRONMENT=Development \
  ocm-import-worker:latest

# Run in background with auto-restart
docker run -d \
  --name ocm-import-worker \
  --restart unless-stopped \
  ocm-import-worker:latest

# Run with resource limits
docker run -d \
  --name ocm-import-worker \
  --cpus="2.0" \
  --memory="4g" \
  ocm-import-worker:latest
```

### Management Commands

```bash
# View logs
docker logs ocm-import-worker
docker logs -f ocm-import-worker  # Follow logs

# Stop container
docker stop ocm-import-worker

# Start container
docker start ocm-import-worker

# Restart container
docker restart ocm-import-worker

# Remove container
docker rm -f ocm-import-worker

# Execute command in running container
docker exec -it ocm-import-worker /bin/bash

# View container stats
docker stats ocm-import-worker
```

## Docker Compose

### Using docker-compose.yml

```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Rebuild and restart
docker-compose up -d --build

# Remove volumes
docker-compose down -v
```

### Custom docker-compose file

```bash
# Use a specific compose file
docker-compose -f docker-compose.prod.yml up -d

# Use multiple compose files (merge configurations)
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

## Volumes

The container uses the following volumes:

- `/app/temp` - Temporary files and import cache
- `/app/logs` - Application logs
- `/app/Shapefiles/World` - Geolocation shapefiles (included in image)

### Managing Volumes

```bash
# List volumes
docker volume ls

# Inspect volume
docker volume inspect ocm-import-temp

# Remove volume (when container is stopped)
docker volume rm ocm-import-temp

# Backup volume
docker run --rm \
  -v ocm-import-temp:/data \
  -v $(pwd):/backup \
  busybox tar czf /backup/ocm-import-temp-backup.tar.gz /data
```

## Multi-Architecture Builds

Build for multiple platforms (e.g., ARM64 for Raspberry Pi):

```bash
# Create builder
docker buildx create --name ocm-builder --use

# Build for multiple platforms
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -f Import/OCM.Import.Worker/Dockerfile \
  -t ocm-import-worker:latest \
  --push \
  .
```

## Debugging

### Interactive Shell

```bash
# Start container with shell
docker run -it --rm \
  --entrypoint /bin/bash \
  ocm-import-worker:latest

# Execute shell in running container
docker exec -it ocm-import-worker /bin/bash
```

### Check Application Files

```bash
# List files in container
docker exec ocm-import-worker ls -la /app

# Check appsettings.json
docker exec ocm-import-worker cat /app/appsettings.json

# Check environment variables
docker exec ocm-import-worker env
```

### Memory and Performance

```bash
# View resource usage
docker stats ocm-import-worker

# View processes in container
docker exec ocm-import-worker ps aux

# Check disk usage
docker exec ocm-import-worker df -h
```

## Troubleshooting

### Container Won't Start

```bash
# Check container logs
docker logs ocm-import-worker

# Check container status
docker ps -a | grep ocm-import-worker

# Inspect container
docker inspect ocm-import-worker
```

### Configuration Issues

```bash
# Verify environment variables
docker exec ocm-import-worker env | grep DOTNET

# Check mounted volumes
docker exec ocm-import-worker ls -la /app/temp
docker exec ocm-import-worker ls -la /app/Shapefiles/World
```

### Performance Issues

```bash
# Check resource limits
docker inspect ocm-import-worker | grep -A 10 "Resources"

# Monitor in real-time
docker stats ocm-import-worker

# Check system logs
docker system df
docker system events
```

## Production Deployment

### Recommended Configuration

```yaml
services:
  ocm-import-worker:
    image: ocm-import-worker:latest
    restart: unless-stopped
    environment:
      - DOTNET_ENVIRONMENT=Production
      - KeyVaultName=${AZURE_KEYVAULT_NAME}
      - AZURE_CLIENT_ID=${AZURE_CLIENT_ID}
      - AZURE_CLIENT_SECRET=${AZURE_CLIENT_SECRET}
      - AZURE_TENANT_ID=${AZURE_TENANT_ID}
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 4G
        reservations:
          cpus: '0.5'
          memory: 1G
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
    healthcheck:
      test: ["CMD", "test", "-f", "/app/OCM.Import.Worker.dll"]
      interval: 30s
      timeout: 5s
      retries: 3
```

### Security Best Practices

1. **Run as non-root user** (already configured in Dockerfile)
2. **Use secrets management** (Azure Key Vault or Docker Secrets)
3. **Limit container resources** (CPU and memory limits)
4. **Use specific image tags** instead of `latest`
5. **Scan images for vulnerabilities**

```bash
# Scan image for vulnerabilities
docker scan ocm-import-worker:latest
```

## Image Registry

### Push to Registry

```bash
# Tag image
docker tag ocm-import-worker:latest myregistry.azurecr.io/ocm-import-worker:v1.0.0

# Login to registry
docker login myregistry.azurecr.io

# Push image
docker push myregistry.azurecr.io/ocm-import-worker:v1.0.0
```

### Pull from Registry

```bash
# Pull image
docker pull myregistry.azurecr.io/ocm-import-worker:v1.0.0

# Run pulled image
docker run -d myregistry.azurecr.io/ocm-import-worker:v1.0.0
```

## Cleanup

```bash
# Stop and remove container
docker stop ocm-import-worker && docker rm ocm-import-worker

# Remove image
docker rmi ocm-import-worker:latest

# Clean up unused resources
docker system prune -a

# Remove all OCM-related containers and volumes
docker-compose down -v
docker volume rm ocm-import-temp ocm-import-logs
```

## Support

For issues or questions:
- Check the logs: `docker logs ocm-import-worker`
- Open an issue on GitHub: https://github.com/openchargemap/ocm-system
- Review the OCM documentation

## Version History

- v1.0.0 - Initial Docker support for .NET 9
  - Multi-stage build
  - Non-root user
  - Volume support for temp files and logs
  - Docker Compose support
