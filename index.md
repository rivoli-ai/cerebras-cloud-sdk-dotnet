# Cerebras.Cloud.Sdk API Documentation

## Overview

This is the API documentation for the Cerebras.Cloud.Sdk (Unofficial) - a .NET SDK for the Cerebras Cloud API.

## Quick Links

- [API Reference](api/index.md) - Complete API documentation
- [GitHub Repository](https://github.com/rivoli-ai/cerebras-cloud-sdk-dotnet)
- [NuGet Package](https://www.nuget.org/packages/Cerebras.Cloud.Sdk.Unofficial)

## Getting Started

```csharp
// Install the package
// dotnet add package Cerebras.Cloud.Sdk.Unofficial

// Basic usage
var services = new ServiceCollection();
services.AddCerebrasClientV2(options =>
{
    options.ApiKey = "your-api-key-here";
});

var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<ICerebrasClientV2>();
```

## Main Namespaces

- **Cerebras.Cloud.Sdk** - Core client interfaces and implementations
- **Cerebras.Cloud.Sdk.Chat** - Chat completion functionality
- **Cerebras.Cloud.Sdk.Completions** - Text completion functionality
- **Cerebras.Cloud.Sdk.Models** - Model information APIs
- **Cerebras.Cloud.Sdk.Configuration** - Configuration options
- **Cerebras.Cloud.Sdk.Exceptions** - Exception types