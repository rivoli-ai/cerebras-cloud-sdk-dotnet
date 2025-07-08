# Cerebras.Cloud.Sdk (Unofficial)

An unofficial .NET SDK for the Cerebras Cloud API, providing easy integration with Cerebras AI models in your .NET applications.

📚 **[API Documentation](https://rivoli-ai.github.io/cerebras-cloud-sdk-dotnet/)** | 📦 **[NuGet Package](https://www.nuget.org/packages/Cerebras.Cloud.Sdk.Unofficial)** | 🐙 **[GitHub Repository](https://github.com/rivoli-ai/cerebras-cloud-sdk-dotnet)**

## Features

- **Chat Completions**: Full support for chat-based completions with system, user, and assistant messages
- **Text Completions**: Traditional text completion API for prompt-based generation
- **Streaming Support**: Real-time streaming responses for both chat and text completions
- **Models API**: List and retrieve information about available models
- **Advanced Features**: Tool/function calling, JSON mode, logit bias, and more
- **Robust Error Handling**: Built-in retry logic with exponential backoff and typed exceptions
- **Dependency Injection**: First-class support for ASP.NET Core dependency injection
- **Comprehensive Logging**: Detailed logging for debugging and monitoring
- **Type-safe Models**: Strongly typed request/response models with IntelliSense support

## Installation

```bash
dotnet add package Cerebras.Cloud.Sdk.Unofficial
```

## Quick Start

### Basic Usage - Chat Completions

```csharp
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Chat;

// Option 1: Using the enhanced client (recommended)
var services = new ServiceCollection();
services.AddCerebrasClientV2(options =>
{
    options.ApiKey = "your-api-key-here"; // Or set via CEREBRAS_API_KEY environment variable
});

var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<ICerebrasClientV2>();

// Create a chat completion
var request = new ChatCompletionRequest
{
    Model = "llama-3.3-70b",
    Messages = new List<ChatMessage>
    {
        new() { Role = "system", Content = "You are a helpful assistant." },
        new() { Role = "user", Content = "What is the capital of France?" }
    },
    MaxTokens = 100,
    Temperature = 0.7
};

var response = await client.Chat.CreateAsync(request);
Console.WriteLine(response.Choices[0].Message.Content);
```

### Text Completions

```csharp
using Cerebras.Cloud.Sdk.Completions;

// Generate text completion
var request = new TextCompletionRequest
{
    Model = "llama-3.3-70b",
    Prompt = "The capital of France is",
    MaxTokens = 100,
    Temperature = 0.7
};

var response = await client.Completions.CreateAsync(request);
Console.WriteLine(response.Choices[0].Text);
```

### Streaming Responses

```csharp
// Chat streaming
var chatRequest = new ChatCompletionRequest
{
    Model = "llama-3.3-70b",
    Messages = new List<ChatMessage>
    {
        new() { Role = "user", Content = "Tell me a story" }
    },
    MaxTokens = 500,
    Stream = true
};

await foreach (var chunk in client.Chat.CreateStreamAsync(chatRequest))
{
    if (chunk.Choices[0].Delta?.Content != null)
    {
        Console.Write(chunk.Choices[0].Delta.Content);
    }
}

// Text completion streaming
var textRequest = new TextCompletionRequest
{
    Model = "llama-3.3-70b",
    Prompt = "Once upon a time",
    MaxTokens = 500,
    Stream = true
};

await foreach (var chunk in client.Completions.CreateStreamAsync(textRequest))
{
    Console.Write(chunk.Choices[0].Text);
}
```

### Dependency Injection

```csharp
// In your Program.cs or Startup.cs
builder.Services.AddCerebrasClientV2(builder.Configuration);

// Or with custom configuration
builder.Services.AddCerebrasClientV2(options =>
{
    options.ApiKey = "your-api-key";
    options.DefaultModel = "llama-3.3-70b";
    options.TimeoutSeconds = 60;
});

// Then inject ICerebrasClientV2 wherever needed
public class MyService
{
    private readonly ICerebrasClientV2 _client;
    
    public MyService(ICerebrasClientV2 client)
    {
        _client = client;
    }
    
    public async Task<string> GenerateResponseAsync(string prompt)
    {
        var response = await _client.Chat.CreateAsync(new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = prompt }
            }
        });
        
        return response.Choices[0].Message.Content;
    }
}
```

### Legacy Client Support

The original `ICerebrasClient` interface is still supported for backward compatibility:

```csharp
// Using the legacy client
services.AddCerebrasClient(configuration);

// Inject and use
public class LegacyService
{
    private readonly ICerebrasClient _client;
    
    public LegacyService(ICerebrasClient client)
    {
        _client = client;
    }
}
```

## Configuration

The SDK can be configured through code or via appsettings.json:

```json
{
  "CerebrasClient": {
    "ApiKey": "your-api-key-here",
    "BaseUrl": "https://api.cerebras.ai/v1/",
    "DefaultModel": "llama-3.3-70b",
    "DefaultTemperature": 0.7,
    "DefaultMaxTokens": 1024,
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "EnableLogging": true,
    "RateLimit": {
      "Enabled": true,
      "RequestsPerMinute": 60,
      "TokensPerMinute": 100000
    }
  }
}
```

## Available Models

List all available models:

```csharp
var models = await client.Models.ListAsync();
foreach (var model in models)
{
    Console.WriteLine($"{model.Id}: {model.Name} (Context: {model.ContextWindow} tokens)");
}
```

Get specific model details:

```csharp
var model = await client.Models.RetrieveAsync("llama-3.3-70b");
Console.WriteLine($"Model: {model.Name}");
Console.WriteLine($"Context window: {model.ContextWindow}");
Console.WriteLine($"Description: {model.Description}");
```

## Advanced Features

### Tool/Function Calling

```csharp
var request = new ChatCompletionRequest
{
    Model = "llama-3.3-70b",
    Messages = new List<ChatMessage>
    {
        new() { Role = "user", Content = "What's the weather in Paris?" }
    },
    Tools = new List<ChatTool>
    {
        new()
        {
            Type = "function",
            Function = new ChatFunction
            {
                Name = "get_weather",
                Description = "Get the current weather for a location",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        location = new { type = "string", description = "City name" }
                    },
                    required = new[] { "location" }
                }
            }
        }
    }
};

var response = await client.Chat.CreateAsync(request);
```

### JSON Mode

```csharp
var request = new ChatCompletionRequest
{
    Model = "llama-3.3-70b",
    Messages = new List<ChatMessage>
    {
        new() { Role = "user", Content = "List 3 programming languages as JSON" }
    },
    ResponseFormat = new ResponseFormat { Type = "json_object" }
};
```

## Error Handling

The SDK provides comprehensive error handling with typed exceptions:

```csharp
try
{
    var response = await client.Chat.CreateAsync(request);
}
catch (CerebrasApiException ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
    Console.WriteLine($"Status Code: {ex.StatusCode}");
    Console.WriteLine($"Error Type: {ex.ErrorType}");
    Console.WriteLine($"Error Code: {ex.ErrorCode}");
    
    // Handle specific error types
    switch (ex.StatusCode)
    {
        case HttpStatusCode.Unauthorized:
            Console.WriteLine("Invalid API key");
            break;
        case HttpStatusCode.TooManyRequests:
            Console.WriteLine("Rate limit exceeded");
            break;
        case HttpStatusCode.BadRequest:
            Console.WriteLine($"Invalid request: {ex.Message}");
            break;
    }
}
catch (TaskCanceledException)
{
    Console.WriteLine("Request timed out");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Network error: {ex.Message}");
}
```

## Building and Testing

### Building the SDK

```bash
# Build the SDK
./scripts/build.sh

# Create NuGet package
./scripts/pack.sh
```

### Running Tests

```bash
# Run all tests (unit + integration if API key is set)
./scripts/run-tests.sh

# Run only unit tests
dotnet test --filter "FullyQualifiedName~Unit"

# Run integration tests (requires CEREBRAS_API_KEY environment variable)
export CEREBRAS_API_KEY=your-api-key
dotnet test --filter "FullyQualifiedName~Integration"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
# Generate HTML report
reportgenerator -reports:./TestResults/*/coverage.cobertura.xml -targetdir:./coverage-report -reporttypes:Html
```

### Running Examples

```bash
# Set your API key
export CEREBRAS_API_KEY=your-api-key

# Run the quick start example
./scripts/run-example.sh

# Or run specific examples directly
dotnet run --project examples/Cerebras.Cloud.Sdk.Examples/Cerebras.Cloud.Sdk.Examples.csproj
```

## API Coverage

This SDK implements the complete Cerebras Cloud API surface:

- **Chat Completions** (`/v1/chat/completions`)
  - Streaming and non-streaming
  - Tool/function calling
  - JSON mode
  - System messages
  - All parameters (temperature, top_p, frequency_penalty, etc.)
  
- **Text Completions** (`/v1/completions`)
  - Streaming and non-streaming
  - Multiple prompts
  - Echo mode
  - Suffix support
  - Logit bias
  
- **Models** (`/v1/models`)
  - List available models
  - Retrieve model details
  
- **Error Handling**
  - Typed exceptions
  - Automatic retry with exponential backoff
  - Request timeout configuration

## Contributing

This is an unofficial SDK. Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the Apache License, Version 2.0 - see the LICENSE file for details.

## Disclaimer

This is an unofficial SDK and is not affiliated with, endorsed by, or supported by Cerebras Systems Inc.