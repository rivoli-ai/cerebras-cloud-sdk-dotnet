#!/bin/bash

echo "Running Cerebras.Cloud.Sdk Example"
echo "================================="

# Check if API key is set
if [ -z "$CEREBRAS_API_KEY" ]; then
    echo ""
    echo "ERROR: CEREBRAS_API_KEY environment variable not set"
    echo "Please set your API key:"
    echo "  export CEREBRAS_API_KEY=your-api-key"
    exit 1
fi

# Build the example
echo ""
echo "Building example project..."
dotnet build examples/QuickStart/QuickStart.csproj

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

# Run the example
echo ""
echo "Running example..."
echo ""
dotnet run --project examples/QuickStart/QuickStart.csproj