#!/bin/bash

echo "Creating NuGet Package for Cerebras.Cloud.Sdk"
echo "============================================"

# Ensure we're in Release mode
echo "Building in Release mode..."
dotnet build -c Release src/Cerebras.Cloud.Sdk/Cerebras.Cloud.Sdk.csproj

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

# Create the package
echo ""
echo "Creating NuGet package..."
dotnet pack src/Cerebras.Cloud.Sdk/Cerebras.Cloud.Sdk.csproj -c Release -o ./nupkg

if [ $? -eq 0 ]; then
    echo ""
    echo "Package created successfully!"
    echo "Package location: ./nupkg/"
    ls -la ./nupkg/*.nupkg
else
    echo ""
    echo "Package creation failed!"
    exit 1
fi