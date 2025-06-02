#!/bin/bash

# Build script for Quasar in Docker container
echo "===== Building Quasar with Docker ====="
echo "Current directory: $(pwd)"
echo "Listing files:"
ls -la

# Ensure NuGet packages are restored
echo "===== Restoring NuGet packages ====="
nuget restore Quasar.sln

# Build the solution
echo "===== Building solution in Release mode ====="
msbuild Quasar.sln /p:Configuration=Release /p:Platform=AnyCPU

# Check build result
if [ $? -eq 0 ]; then
    echo "===== Build successful! ====="
    echo "Output files in bin/Release directory:"
    find bin/Release -type f -name "*.exe" -o -name "*.dll" | sort
else
    echo "===== Build failed! ====="
    exit 1
fi

# Set permissions so that files can be accessed outside the container
echo "===== Setting permissions for output files ====="
find bin -type f -exec chmod 644 {} \;
find bin -type d -exec chmod 755 {} \;

echo "===== Build process completed ====="
