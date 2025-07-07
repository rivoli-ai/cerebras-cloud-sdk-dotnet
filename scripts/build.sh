#!/bin/bash

echo "Building Cerebras.Cloud.Sdk"
echo "=========================="

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean -c Release

# Restore packages
echo ""
echo "Restoring packages..."
dotnet restore

# Build in Release mode
echo ""
echo "Building solution..."
dotnet build -c Release

# Build succeeded?
if [ $? -eq 0 ]; then
    echo ""
    echo "Build completed successfully!"
else
    echo ""
    echo "Build failed!"
    exit 1
fi