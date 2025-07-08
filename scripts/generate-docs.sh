#!/bin/bash

# Script to generate API documentation using DocFX

set -e

echo "🔧 Installing DocFX if not already installed..."
if ! command -v docfx &> /dev/null; then
    dotnet tool install -g docfx
    echo "✅ DocFX installed"
else
    echo "✅ DocFX already installed"
fi

echo "📦 Restoring dependencies..."
dotnet restore

echo "📝 Generating API documentation..."
docfx docfx.json --serve:no

echo "✅ Documentation generated in _site folder"
echo ""
echo "To preview the documentation locally, run:"
echo "  docfx docfx.json --serve"
echo ""
echo "Or open _site/index.html in your browser"