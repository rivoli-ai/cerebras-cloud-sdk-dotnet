#!/bin/bash

# Script to run tests with code coverage

echo "Running tests with code coverage..."

# Restore packages
echo "Restoring packages..."
dotnet restore

# Run tests with coverage
echo "Running unit tests with coverage..."
dotnet test tests/Cerebras.Cloud.Sdk.Tests/Cerebras.Cloud.Sdk.Tests.csproj \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=cobertura \
    /p:CoverletOutput=./coverage/ \
    /p:Exclude="[xunit.*]*%2c[*]*.Tests.*" \
    --filter "FullyQualifiedName!~Integration" \
    --no-build

# Generate coverage report
echo "Generating coverage report..."
dotnet tool install -g dotnet-reportgenerator-globaltool 2>/dev/null || true
reportgenerator \
    -reports:tests/Cerebras.Cloud.Sdk.Tests/coverage/coverage.cobertura.xml \
    -targetdir:coverage-report \
    -reporttypes:Html

echo "Coverage report generated in coverage-report/index.html"

# Display coverage summary
echo ""
echo "Coverage Summary:"
echo "================="
dotnet test tests/Cerebras.Cloud.Sdk.Tests/Cerebras.Cloud.Sdk.Tests.csproj \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:CoverletOutput=./coverage/ \
    --filter "FullyQualifiedName!~Integration" \
    --no-build \
    --no-restore | grep -E "(Total|Line|Branch|Method)" || true

echo ""
echo "To run integration tests separately (requires API key):"
echo "  dotnet test --filter \"FullyQualifiedName~Integration\""