# Cerebras Cloud SDK Examples

This directory contains example applications demonstrating the Cerebras Cloud SDK for .NET.

## Important Note on SDK Versions

The Cerebras SDK provides two client interfaces:
- **Modern SDK (`ICerebrasClientV2`)** - Recommended, with full feature support
- **Legacy SDK (`ICerebrasClient`)** - For backward compatibility, limited features

Both SDKs communicate with the same Cerebras API v1 endpoints at `https://api.cerebras.ai/v1/`. The difference is in the SDK architecture and features, not the API version.

## Available Examples

### 1. QuickStart
A comprehensive example showing all major features of the modern SDK:
- Simple chat completions
- Streaming chat completions
- Text completions
- Tool calling
- Model listing
- Backward compatibility with legacy API

**Run it:**
```bash
cd QuickStart
dotnet run
```

### 2. ToolCalling
Detailed examples of the tool calling feature (modern SDK only):
- Simple tool calling
- Multiple tools in one request
- Tool calling with follow-up responses
- Streaming with tools

**Run it:**
```bash
cd ToolCalling
dotnet run
```

### 3. VerifyModels
A utility to verify the models endpoint works correctly with both SDK versions:
- Lists all available models
- Retrieves individual model details
- Supports both Legacy and Modern SDKs

**Run it:**
```bash
cd VerifyModels
dotnet run          # Uses Legacy SDK
dotnet run --v2     # Uses Modern SDK
```

### 4. ApiComparison
Side-by-side comparison of Legacy SDK vs Modern SDK:
- Shows equivalent code for both SDK versions
- Highlights modern SDK exclusive features
- Demonstrates migration paths

**Run it:**
```bash
cd ApiComparison
dotnet run
```

## SDK Versions

The Cerebras SDK provides two client interfaces (both use the same API endpoints):

### Legacy SDK (`ICerebrasClient`)
- Simple, direct methods on the client interface
- Basic completion support
- Suitable for simple use cases
- Use `services.AddCerebrasClient()`

### Modern SDK (`ICerebrasClientV2`)
- Service-oriented architecture
- Full chat completion support
- Tool/function calling
- Richer response models
- Backward compatible with legacy SDK
- Use `services.AddCerebrasClientV2()`

## Prerequisites

1. Set your API key:
   ```bash
   export CEREBRAS_API_KEY=csk-1234567890abcdef1234567890abcdef
   ```

2. Have .NET 8.0 or later installed

## Common Patterns

### Using Modern SDK with Dependency Injection

```csharp
// In Startup/Program.cs
services.AddCerebrasClientV2(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");
    options.DefaultModel = "llama-3.3-70b";
});

// In your service
public class MyService
{
    private readonly ICerebrasClientV2 _client;
    
    public MyService(ICerebrasClientV2 client)
    {
        _client = client;
    }
    
    public async Task<string> GenerateResponse(string prompt)
    {
        var response = await _client.Chat.CreateAsync(new ChatCompletionRequest
        {
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = prompt }
            }
        });
        
        return response.Choices[0].Message.Content;
    }
}
```

### Migrating from Legacy to Modern SDK

```csharp
// Legacy SDK
var response = await client.GenerateCompletionAsync(new CompletionRequest
{
    Model = "llama-3.3-70b",
    Prompt = "Hello, world!",
    MaxTokens = 100
});

// Modern SDK - Option 1: Use compatibility method
var response = await client.GenerateCompletionAsync(new CompletionRequest
{
    Model = "llama-3.3-70b",
    Prompt = "Hello, world!",
    MaxTokens = 100
});

// Modern SDK - Option 2: Use new chat API (recommended)
var response = await client.Chat.CreateAsync(new ChatCompletionRequest
{
    Model = "llama-3.3-70b",
    Messages = new List<ChatMessage>
    {
        new() { Role = "user", Content = "Hello, world!" }
    },
    MaxTokens = 100
});
```

## Features by SDK Version

| Feature | Legacy SDK | Modern SDK |
|---------|------------|------------|
| Text Completions | ✅ | ✅ |
| Chat Completions | ❌ | ✅ |
| Streaming | ✅ | ✅ |
| Tool/Function Calling | ❌ | ✅ |
| Model Listing | ✅ | ✅ |
| Structured Responses | ❌ | ✅ |
| Multiple Choices (n > 1) | ❌ | ✅ |
| Logprobs | ❌ | ✅ |

## Best Practices

1. **Use Modern SDK for new projects** - It provides more features and better structure
2. **Set appropriate timeouts** for long-running requests
3. **Handle rate limits** gracefully with retry logic
4. **Use streaming** for better user experience with long responses
5. **Validate tool calls** before executing them
6. **Log API usage** for monitoring and debugging

## Troubleshooting

### API Key Issues
```bash
# Check if API key is set
echo $CEREBRAS_API_KEY

# Set it if missing
export CEREBRAS_API_KEY=csk-1234567890abcdef1234567890abcdef
```

### Connection Issues
- Verify internet connectivity
- Check if the API endpoint is accessible
- Ensure firewall/proxy settings allow HTTPS requests

### Model Not Found
- Use `ListModelsAsync()` to see available models
- Ensure you're using the exact model ID
- Check if the model is marked as available

## Additional Resources

- [Cerebras Cloud SDK Documentation](../README.md)
- [API Reference](../api/index.md)
- [Cerebras Cloud Platform](https://cloud.cerebras.ai)