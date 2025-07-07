#!/bin/bash

echo "Running Cerebras.Cloud.Sdk Tests"
echo "================================"

# Run unit tests
echo ""
echo "Running Unit Tests..."
dotnet test --filter "FullyQualifiedName~Unit" --logger "console;verbosity=normal"

# Check if CEREBRAS_API_KEY is set for integration tests
if [ -z "$CEREBRAS_API_KEY" ]; then
    echo ""
    echo "Skipping Integration Tests (CEREBRAS_API_KEY not set)"
    echo "To run integration tests, set your API key:"
    echo "  export CEREBRAS_API_KEY=your-api-key"
else
    echo ""
    echo "Running Integration Tests..."
    dotnet test --filter "FullyQualifiedName~Integration" --logger "console;verbosity=normal"
fi