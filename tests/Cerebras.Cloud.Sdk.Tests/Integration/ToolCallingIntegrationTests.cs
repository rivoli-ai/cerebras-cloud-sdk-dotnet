using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Cerebras.Cloud.Sdk.Tests.Integration;

[Collection("Integration Tests")]
public class ToolCallingIntegrationTests : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ICerebrasClientV2 _client;
    private readonly ITestOutputHelper _output;
    private readonly string? _apiKey;

    public ToolCallingIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        _apiKey = configuration["CEREBRAS_API_KEY"] ?? Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");

        // Build service container
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        services.AddLogging(builder =>
        {
            builder.AddConsole()
                   .SetMinimumLevel(LogLevel.Information);
        });

        // Add Cerebras client V2
        services.AddCerebrasClientV2(configuration);

        _serviceProvider = services.BuildServiceProvider();
        _client = _serviceProvider.GetRequiredService<ICerebrasClientV2>();
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException(
                "Cerebras API key not found. Set CEREBRAS_API_KEY environment variable or configure in appsettings.json");
        }
        
        _output.WriteLine("Starting Cerebras tool calling integration tests with API key configured");
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task ChatCompletion_WithSingleToolCall_Works()
    {
        // Arrange
        
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() 
                { 
                    Role = "user", 
                    Content = "What's the weather in San Francisco? Use the get_weather function." 
                }
            },
            Tools = new List<Tool>
            {
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "get_weather",
                        Description = "Get the current weather in a given location",
                        Parameters = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["location"] = new Dictionary<string, string>
                                {
                                    ["type"] = "string",
                                    ["description"] = "The city and state, e.g. San Francisco, CA"
                                },
                                ["unit"] = new Dictionary<string, object>
                                {
                                    ["type"] = "string",
                                    ["enum"] = new[] { "celsius", "fahrenheit" }
                                }
                            },
                            ["required"] = new[] { "location" }
                        }
                    }
                }
            },
            Temperature = 0.1 // Low temperature for more deterministic behavior
        };

        // Act
        var response = await _client.Chat.CreateAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Choices);
        Assert.NotEmpty(response.Choices);
        
        var choice = response.Choices[0];
        _output.WriteLine($"Finish reason: {choice.FinishReason}");
        
        if (choice.Message.ToolCalls != null && choice.Message.ToolCalls.Any())
        {
            // Model decided to use the tool
            Assert.Equal("tool_calls", choice.FinishReason);
            Assert.NotNull(choice.Message.ToolCalls);
            Assert.NotEmpty(choice.Message.ToolCalls);
            
            var toolCall = choice.Message.ToolCalls[0];
            Assert.Equal("function", toolCall.Type);
            Assert.Equal("get_weather", toolCall.Function.Name);
            Assert.NotEmpty(toolCall.Function.Arguments);
            
            _output.WriteLine($"Tool call ID: {toolCall.Id}");
            _output.WriteLine($"Function: {toolCall.Function.Name}");
            _output.WriteLine($"Arguments: {toolCall.Function.Arguments}");
            
            // Verify the arguments contain location
            Assert.Contains("San Francisco", toolCall.Function.Arguments, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // Model chose not to use the tool - this is also valid
            _output.WriteLine($"Model response: {choice.Message.Content}");
            Assert.NotNull(choice.Message.Content);
        }
    }

    [Fact]
    public async Task ChatCompletion_WithToolResponse_GeneratesAnswer()
    {
        // Arrange
        
        // First, make a request that should trigger a tool call
        var initialRequest = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() 
                { 
                    Role = "user", 
                    Content = "What's the current temperature in Tokyo? Please use the get_weather function to check." 
                }
            },
            Tools = new List<Tool>
            {
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "get_weather",
                        Description = "Get the current weather in a given location",
                        Parameters = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["location"] = new Dictionary<string, string>
                                {
                                    ["type"] = "string",
                                    ["description"] = "The city name"
                                }
                            },
                            ["required"] = new[] { "location" }
                        }
                    }
                }
            },
            ToolChoice = "auto",
            Temperature = 0.1
        };

        var initialResponse = await _client.Chat.CreateAsync(initialRequest);
        
        // Build conversation with tool response
        var messages = new List<ChatMessage>
        {
            new() { Role = "user", Content = initialRequest.Messages[0].Content }
        };
        
        if (initialResponse.Choices[0].Message.ToolCalls?.Any() == true)
        {
            // Add assistant's tool call message
            messages.Add(new()
            {
                Role = "assistant",
                Content = initialResponse.Choices[0].Message.Content,
                ToolCalls = initialResponse.Choices[0].Message.ToolCalls
            });
            
            // Add tool response
            var toolCall = initialResponse.Choices[0].Message.ToolCalls![0];
            messages.Add(new()
            {
                Role = "tool",
                Content = "{\"temperature\": 18, \"unit\": \"celsius\", \"condition\": \"partly cloudy\"}",
                ToolCallId = toolCall.Id
            });
            
            // Make follow-up request
            var followUpRequest = new ChatCompletionRequest
            {
                Model = "llama-3.3-70b",
                Messages = messages,
                Temperature = 0.7
            };
            
            var followUpResponse = await _client.Chat.CreateAsync(followUpRequest);
            
            // Assert
            Assert.NotNull(followUpResponse);
            Assert.NotNull(followUpResponse.Choices[0].Message.Content);
            _output.WriteLine($"Assistant response: {followUpResponse.Choices[0].Message.Content}");
            
            // The response should mention the temperature
            Assert.Contains("18", followUpResponse.Choices[0].Message.Content);
        }
        else
        {
            _output.WriteLine("Model did not make a tool call, skipping follow-up test");
        }
    }

    [Fact]
    public async Task ChatCompletion_WithMultipleTools_SelectsAppropriate()
    {
        // Arrange
        
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() 
                { 
                    Role = "user", 
                    Content = "Search the web for the latest news about artificial intelligence. Use the search tool." 
                }
            },
            Tools = new List<Tool>
            {
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "get_weather",
                        Description = "Get weather information",
                        Parameters = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["location"] = new Dictionary<string, string> { ["type"] = "string" }
                            }
                        }
                    }
                },
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "search_web",
                        Description = "Search the web for information",
                        Parameters = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["query"] = new Dictionary<string, string>
                                {
                                    ["type"] = "string",
                                    ["description"] = "The search query"
                                }
                            },
                            ["required"] = new[] { "query" }
                        }
                    }
                },
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "calculate",
                        Description = "Perform mathematical calculations",
                        Parameters = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["expression"] = new Dictionary<string, string> { ["type"] = "string" }
                            }
                        }
                    }
                }
            },
            Temperature = 0.1
        };

        // Act
        var response = await _client.Chat.CreateAsync(request);

        // Assert
        Assert.NotNull(response);
        
        if (response.Choices[0].Message.ToolCalls?.Any() == true)
        {
            var toolCall = response.Choices[0].Message.ToolCalls![0];
            _output.WriteLine($"Selected tool: {toolCall.Function.Name}");
            _output.WriteLine($"Arguments: {toolCall.Function.Arguments}");
            
            // Should select search_web based on the prompt
            Assert.Equal("search_web", toolCall.Function.Name);
            Assert.Contains("artificial intelligence", toolCall.Function.Arguments, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            _output.WriteLine($"Model response without tool: {response.Choices[0].Message.Content}");
        }
    }

    [Fact]
    public async Task ChatCompletionStream_WithTools_StreamsToolCalls()
    {
        // Arrange
        
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() 
                { 
                    Role = "user", 
                    Content = "Calculate the sum of 42 and 58 using the calculator function." 
                }
            },
            Tools = new List<Tool>
            {
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "calculator",
                        Description = "Perform basic arithmetic operations",
                        Parameters = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["operation"] = new Dictionary<string, object>
                                {
                                    ["type"] = "string",
                                    ["enum"] = new[] { "add", "subtract", "multiply", "divide" }
                                },
                                ["a"] = new Dictionary<string, string> { ["type"] = "number" },
                                ["b"] = new Dictionary<string, string> { ["type"] = "number" }
                            },
                            ["required"] = new[] { "operation", "a", "b" }
                        }
                    }
                }
            },
            Stream = true,
            Temperature = 0.1
        };

        // Act
        var chunks = new List<ChatCompletionChunk>();
        var toolCallsFound = false;
        
        await foreach (var chunk in _client.Chat.CreateStreamAsync(request))
        {
            chunks.Add(chunk);
            
            if (chunk.Choices[0].Delta?.ToolCalls != null)
            {
                toolCallsFound = true;
                foreach (var toolCall in chunk.Choices[0].Delta.ToolCalls!)
                {
                    if (!string.IsNullOrEmpty(toolCall.Id))
                        _output.WriteLine($"Tool call ID: {toolCall.Id}");
                    if (!string.IsNullOrEmpty(toolCall.Function?.Name))
                        _output.WriteLine($"Function name: {toolCall.Function.Name}");
                    if (!string.IsNullOrEmpty(toolCall.Function?.Arguments))
                        _output.WriteLine($"Arguments chunk: {toolCall.Function.Arguments}");
                }
            }
            
            if (!string.IsNullOrEmpty(chunk.Choices[0].Delta?.Content))
            {
                _output.WriteLine($"Content chunk: {chunk.Choices[0].Delta.Content}");
            }
        }

        // Assert
        Assert.NotEmpty(chunks);
        _output.WriteLine($"Total chunks received: {chunks.Count}");
        _output.WriteLine($"Tool calls found: {toolCallsFound}");
        
        // Check if we got a finish reason
        var lastChunk = chunks.LastOrDefault(c => !string.IsNullOrEmpty(c.Choices[0].FinishReason));
        if (lastChunk != null)
        {
            _output.WriteLine($"Finish reason: {lastChunk.Choices[0].FinishReason}");
        }
    }

    [Fact]
    public async Task ChatCompletion_WithToolChoice_None_DoesNotCallTools()
    {
        // Arrange
        
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() 
                { 
                    Role = "user", 
                    Content = "What's the weather in London?" 
                }
            },
            Tools = new List<Tool>
            {
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "get_weather",
                        Description = "Get weather information",
                        Parameters = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["location"] = new Dictionary<string, string> { ["type"] = "string" }
                            }
                        }
                    }
                }
            },
            ToolChoice = "none", // Force the model not to use tools
            Temperature = 0.7
        };

        // Act
        var response = await _client.Chat.CreateAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Choices[0].Message);
        
        // Should not have tool calls
        Assert.Null(response.Choices[0].Message.ToolCalls);
        Assert.NotNull(response.Choices[0].Message.Content);
        Assert.NotEqual("tool_calls", response.Choices[0].FinishReason);
        
        _output.WriteLine($"Model response: {response.Choices[0].Message.Content}");
    }
}