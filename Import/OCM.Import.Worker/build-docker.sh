#!/bin/bash

# OCM Import Worker - Docker Build Script
# This script builds the Docker image from the repository root

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
IMAGE_NAME="ocm-import-worker"
IMAGE_TAG="${1:-latest}"
BUILD_CONFIG="${BUILD_CONFIGURATION:-Release}"
DOCKERFILE_PATH="Import/OCM.Import.Worker/Dockerfile"

# Get script directory and navigate to repo root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}OCM Import Worker - Docker Build${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Repository Root: ${YELLOW}$REPO_ROOT${NC}"
echo -e "Image Name: ${YELLOW}$IMAGE_NAME:$IMAGE_TAG${NC}"
echo -e "Build Configuration: ${YELLOW}$BUILD_CONFIG${NC}"
echo -e "Dockerfile: ${YELLOW}$DOCKERFILE_PATH${NC}"
echo ""

# Navigate to repository root
cd "$REPO_ROOT"

# Check if Dockerfile exists
if [ ! -f "$DOCKERFILE_PATH" ]; then
    echo -e "${RED}Error: Dockerfile not found at $DOCKERFILE_PATH${NC}"
    exit 1
fi

# Build the image
echo -e "${GREEN}Building Docker image...${NC}"
docker build \
    -f "$DOCKERFILE_PATH" \
    --build-arg BUILD_CONFIGURATION="$BUILD_CONFIG" \
    -t "$IMAGE_NAME:$IMAGE_TAG" \
    .

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}Build completed successfully!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo -e "Image: ${YELLOW}$IMAGE_NAME:$IMAGE_TAG${NC}"
    echo ""
    echo "To run the container:"
    echo -e "${YELLOW}  docker run -d --name ocm-import-worker $IMAGE_NAME:$IMAGE_TAG${NC}"
    echo ""
    echo "To view logs:"
    echo -e "${YELLOW}  docker logs -f ocm-import-worker${NC}"
    echo ""
    echo "To use Docker Compose:"
    echo -e "${YELLOW}  cd Import/OCM.Import.Worker && docker-compose up -d${NC}"
    echo ""
else
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi
