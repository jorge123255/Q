#!/bin/bash

# Quasar Docker Build Wrapper
# This script builds the Quasar solution using Docker

# Set the project root directory
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$PROJECT_ROOT"

echo "===== Quasar Docker Build ====="
echo "Project root: $PROJECT_ROOT"

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "Error: Docker is not installed or not in PATH. Please install Docker Desktop for Mac."
    exit 1
fi

# Build the Docker image if it doesn't exist
echo "===== Building Docker image if needed ====="
if ! docker images | grep -q "quasar-builder"; then
    echo "Building Docker image..."
    docker build -t quasar-builder .
fi

# Run the Docker container with the source mounted
echo "===== Running build in Docker container ====="
docker run --rm -v "$PROJECT_ROOT:/src" quasar-builder

# Check if build was successful
if [ $? -eq 0 ]; then
    echo "===== Build completed successfully! ====="
    
    # Check if bin/Release directory exists and has files
    if [ -d "$PROJECT_ROOT/bin/Release" ]; then
        echo "Built files:"
        ls -la "$PROJECT_ROOT/bin/Release"
        
        # Check for the client and server executables
        if [ -f "$PROJECT_ROOT/bin/Release/Client.exe" ]; then
            echo "✅ Client.exe built successfully"
        else
            echo "❌ Client.exe not found in bin/Release"
        fi
        
        if [ -f "$PROJECT_ROOT/bin/Release/Quasar.Server.exe" ]; then
            echo "✅ Quasar.Server.exe built successfully"
        else
            echo "❌ Quasar.Server.exe not found in bin/Release"
        fi
        
        # Ask if user wants to build the macOS app bundle
        echo ""
        echo "Would you like to build the macOS app bundle? (y/n)"
        read -r build_mac
        
        if [[ $build_mac == "y" || $build_mac == "Y" ]]; then
            echo "===== Building macOS app bundle ====="
            "$PROJECT_ROOT/Quasar.Server.Mac/build_mac_app.sh"
        fi
    else
        echo "❌ bin/Release directory not found. Build may have failed."
    fi
else
    echo "❌ Build failed."
    exit 1
fi

echo ""
echo "===== Build Process Complete ====="
