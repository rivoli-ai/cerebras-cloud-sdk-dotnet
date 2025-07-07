using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Chat;
using Cerebras.Cloud.Sdk.Completions;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    private static ICerebrasClientV2 _client = null!;
    private static ILogger<Program> _logger = null!;

    static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Configure Cerebras client V2
        services.AddCerebrasClientV2(options =>
        {
            // API key can be set here or via CEREBRAS_API_KEY environment variable
            options.ApiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");
            options.DefaultModel = "llama-3.3-70b";
            options.DefaultTemperature = 0.7;
            options.DefaultMaxTokens = 1024;
        });

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Get the client and logger
        _client = serviceProvider.GetRequiredService<ICerebrasClientV2>();
        _logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            Console.WriteLine("=== Cerebras Cloud SDK Examples ===\n");
            
            // Run all examples
            await Example1_SimpleChatCompletion();
            await Example2_StreamingChatCompletion();
            await Example3_TextCompletion();
            await Example4_ToolCalling();
            await Example5_ListModels();
            await Example6_BackwardCompatibility(serviceProvider);
            
            Console.WriteLine("\n=== All examples completed successfully! ===");
        }
        catch (CerebrasApiException ex)
        {
            _logger.LogError(ex, "API error occurred");
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.StatusCode.HasValue)
            {
                Console.WriteLine($"Status Code: {ex.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred");
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }

    private static async Task Example1_SimpleChatCompletion()
    {
        Console.WriteLine("Example 1: Simple Chat Completion");
        Console.WriteLine("---------------------------------");
        
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = "You are a helpful assistant." },
                new() { Role = "user", Content = "What is the capital of France? Answer in one sentence." }
            },
            MaxTokens = 50,
            Temperature = 0.1
        };

        var response = await _client.Chat.CreateAsync(request);
        
        Console.WriteLine($"Assistant: {response.Choices[0].Message.Content}");
        Console.WriteLine($"Tokens used: {response.Usage?.TotalTokens ?? 0}");
        Console.WriteLine();
    }

    private static async Task Example2_StreamingChatCompletion()
    {
        Console.WriteLine("Example 2: Streaming Chat Completion");
        Console.WriteLine("------------------------------------");
        
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Write a haiku about programming." }
            },
            MaxTokens = 100,
            Temperature = 0.7,
            Stream = true
        };

        Console.Write("Assistant: ");
        await foreach (var chunk in _client.Chat.CreateStreamAsync(request))
        {
            if (chunk.Choices[0].Delta?.Content != null)
            {
                Console.Write(chunk.Choices[0].Delta.Content);
            }
            
            if (chunk.Choices[0].FinishReason != null)
            {
                Console.WriteLine($"\n[Finished: {chunk.Choices[0].FinishReason}]");
            }
        }
        Console.WriteLine();
    }

    private static async Task Example3_TextCompletion()
    {
        Console.WriteLine("Example 3: Text Completion");
        Console.WriteLine("--------------------------");
        
        var request = new TextCompletionRequest
        {
            Model = "llama-3.3-70b",
            Prompt = "The benefits of regular exercise include:",
            MaxTokens = 80,
            Temperature = 0.5
        };

        var response = await _client.Completions.CreateAsync(request);
        
        Console.WriteLine($"Prompt: {request.Prompt}");
        Console.WriteLine($"Completion: {response.Choices[0].Text}");
        Console.WriteLine($"Tokens used: {response.Usage?.TotalTokens ?? 0}");
        Console.WriteLine();
    }

    private static async Task Example4_ToolCalling()
    {
        Console.WriteLine("Example 4: Tool Calling");
        Console.WriteLine("----------------------");
        
        // Define a weather tool
        var tools = new List<Tool>
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
                                ["enum"] = new[] { "celsius", "fahrenheit" },
                                ["description"] = "The temperature unit"
                            }
                        },
                        ["required"] = new[] { "location" }
                    }
                }
            }
        };

        // Initial request
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "What's the weather like in New York?" }
            },
            Tools = tools,
            ToolChoice = "auto",
            Temperature = 0.1
        };

        var response = await _client.Chat.CreateAsync(request);
        
        if (response.Choices[0].Message.ToolCalls?.Count > 0)
        {
            Console.WriteLine("Assistant wants to call a tool:");
            foreach (var toolCall in response.Choices[0].Message.ToolCalls!)
            {
                Console.WriteLine($"  Tool: {toolCall.Function.Name}");
                Console.WriteLine($"  Arguments: {toolCall.Function.Arguments}");
                
                // Parse the arguments
                var args = JsonDocument.Parse(toolCall.Function.Arguments);
                var location = args.RootElement.GetProperty("location").GetString();
                
                // Simulate tool execution
                var weatherData = SimulateWeatherAPI(location!);
                
                // Send tool response back
                var messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "What's the weather like in New York?" },
                    new() 
                    { 
                        Role = "assistant", 
                        Content = response.Choices[0].Message.Content,
                        ToolCalls = response.Choices[0].Message.ToolCalls
                    },
                    new() 
                    { 
                        Role = "tool", 
                        Content = weatherData,
                        ToolCallId = toolCall.Id
                    }
                };
                
                var followUpRequest = new ChatCompletionRequest
                {
                    Model = "llama-3.3-70b",
                    Messages = messages,
                    Temperature = 0.7
                };
                
                var finalResponse = await _client.Chat.CreateAsync(followUpRequest);
                Console.WriteLine($"\nAssistant: {finalResponse.Choices[0].Message.Content}");
            }
        }
        else
        {
            Console.WriteLine($"Assistant: {response.Choices[0].Message.Content}");
        }
        Console.WriteLine();
    }

    private static async Task Example5_ListModels()
    {
        Console.WriteLine("Example 5: List Available Models");
        Console.WriteLine("--------------------------------");
        
        var models = await _client.Models.ListAsync();
        
        Console.WriteLine("Available models:");
        foreach (var model in models)
        {
            Console.WriteLine($"  - {model.Id}: {model.Name}");
            if (!string.IsNullOrEmpty(model.Description))
            {
                Console.WriteLine($"    Description: {model.Description}");
            }
            Console.WriteLine($"    Context window: {model.ContextWindow} tokens");
        }
        Console.WriteLine();
    }

    private static async Task Example6_BackwardCompatibility(IServiceProvider serviceProvider)
    {
        Console.WriteLine("Example 6: Backward Compatibility (Legacy Client)");
        Console.WriteLine("------------------------------------------------");
        
        // Get the legacy client interface
        var legacyClient = serviceProvider.GetRequiredService<ICerebrasClient>();
        
        var response = await legacyClient.GenerateCompletionAsync(new CompletionRequest
        {
            Model = "llama-3.3-70b",
            Prompt = "The meaning of life is",
            MaxTokens = 30,
            Temperature = 0.9
        });
        
        Console.WriteLine($"Legacy completion: {response.Text}");
        Console.WriteLine();
    }

    private static string SimulateWeatherAPI(string location)
    {
        // Simulate a weather API response
        var random = new Random();
        var temp = random.Next(60, 85);
        var conditions = new[] { "sunny", "partly cloudy", "cloudy", "rainy" };
        var condition = conditions[random.Next(conditions.Length)];
        
        return JsonSerializer.Serialize(new
        {
            location = location,
            temperature = temp,
            unit = "fahrenheit",
            condition = condition,
            humidity = random.Next(40, 80),
            wind_speed = random.Next(5, 25),
            wind_direction = "NW"
        });
    }
}