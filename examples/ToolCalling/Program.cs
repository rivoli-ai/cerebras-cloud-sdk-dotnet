using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Chat;
using Cerebras.Cloud.Sdk.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ToolCallingExample;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // Add Cerebras client with configuration
        services.AddCerebrasClientV2(options =>
        {
            options.ApiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY") 
                ?? throw new InvalidOperationException("Please set CEREBRAS_API_KEY environment variable");
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<ICerebrasClientV2>();
        
        Console.WriteLine("=== Cerebras Tool Calling Example ===\n");
        
        // Example 1: Simple tool calling
        await DemoSimpleToolCalling(client);
        
        // Example 2: Multiple tools
        await DemoMultipleTools(client);
        
        // Example 3: Tool calling with follow-up
        await DemoToolCallingWithFollowUp(client);
        
        // Example 4: Streaming with tools
        await DemoStreamingWithTools(client);
    }
    
    private static async Task DemoSimpleToolCalling(ICerebrasClientV2 client)
    {
        Console.WriteLine("1. Simple Tool Calling Demo");
        Console.WriteLine("---------------------------");
        
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() 
                { 
                    Role = "user", 
                    Content = "What's the weather like in New York and San Francisco?" 
                }
            },
            Tools = new List<Tool>
            {
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "get_current_weather",
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
            },
            Temperature = 0.7
        };
        
        var response = await client.Chat.CreateAsync(request);
        
        if (response.Choices[0].Message.ToolCalls != null)
        {
            Console.WriteLine("Assistant wants to call tools:");
            foreach (var toolCall in response.Choices[0].Message.ToolCalls!)
            {
                Console.WriteLine($"  Tool: {toolCall.Function.Name}");
                Console.WriteLine($"  Arguments: {toolCall.Function.Arguments}");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"Assistant: {response.Choices[0].Message.Content}");
        }
        
        Console.WriteLine("\n");
    }
    
    private static async Task DemoMultipleTools(ICerebrasClientV2 client)
    {
        Console.WriteLine("2. Multiple Tools Demo");
        Console.WriteLine("----------------------");
        
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() 
                { 
                    Role = "user", 
                    Content = "What's 25 * 4, and what's the weather in Tokyo?" 
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
                                    ["enum"] = new[] { "add", "subtract", "multiply", "divide" },
                                    ["description"] = "The arithmetic operation"
                                },
                                ["a"] = new Dictionary<string, string>
                                {
                                    ["type"] = "number",
                                    ["description"] = "First number"
                                },
                                ["b"] = new Dictionary<string, string>
                                {
                                    ["type"] = "number",
                                    ["description"] = "Second number"
                                }
                            },
                            ["required"] = new[] { "operation", "a", "b" }
                        }
                    }
                },
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "get_weather",
                        Description = "Get weather information for a location",
                        Parameters = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["location"] = new Dictionary<string, string>
                                {
                                    ["type"] = "string",
                                    ["description"] = "City name"
                                }
                            },
                            ["required"] = new[] { "location" }
                        }
                    }
                }
            },
            Temperature = 0.7
        };
        
        var response = await client.Chat.CreateAsync(request);
        
        if (response.Choices[0].Message.ToolCalls != null)
        {
            Console.WriteLine("Assistant wants to call the following tools:");
            foreach (var toolCall in response.Choices[0].Message.ToolCalls!)
            {
                Console.WriteLine($"  Tool: {toolCall.Function.Name}");
                Console.WriteLine($"  ID: {toolCall.Id}");
                Console.WriteLine($"  Arguments: {toolCall.Function.Arguments}");
                
                // Parse and display arguments
                try
                {
                    var args = JsonDocument.Parse(toolCall.Function.Arguments);
                    Console.WriteLine("  Parsed arguments:");
                    foreach (var prop in args.RootElement.EnumerateObject())
                    {
                        Console.WriteLine($"    {prop.Name}: {prop.Value}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error parsing arguments: {ex.Message}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"Assistant: {response.Choices[0].Message.Content}");
        }
        
        Console.WriteLine("\n");
    }
    
    private static async Task DemoToolCallingWithFollowUp(ICerebrasClientV2 client)
    {
        Console.WriteLine("3. Tool Calling with Follow-up Demo");
        Console.WriteLine("-----------------------------------");
        
        // Step 1: Initial request
        var messages = new List<ChatMessage>
        {
            new() 
            { 
                Role = "user", 
                Content = "What's the weather in Paris? Then tell me what clothes I should wear." 
            }
        };
        
        var tools = new List<Tool>
        {
            new()
            {
                Type = "function",
                Function = new FunctionDefinition
                {
                    Name = "get_weather",
                    Description = "Get current weather for a city",
                    Parameters = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["city"] = new Dictionary<string, string>
                            {
                                ["type"] = "string",
                                ["description"] = "City name"
                            }
                        },
                        ["required"] = new[] { "city" }
                    }
                }
            }
        };
        
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = messages,
            Tools = tools,
            Temperature = 0.7
        };
        
        var response = await client.Chat.CreateAsync(request);
        
        if (response.Choices[0].Message.ToolCalls != null)
        {
            var assistantMessage = response.Choices[0].Message;
            messages.Add(new ChatMessage
            {
                Role = assistantMessage.Role,
                Content = assistantMessage.Content,
                ToolCalls = assistantMessage.ToolCalls
            });
            
            Console.WriteLine("Assistant called tools:");
            foreach (var toolCall in assistantMessage.ToolCalls!)
            {
                Console.WriteLine($"  Calling {toolCall.Function.Name} with {toolCall.Function.Arguments}");
                
                // Simulate tool execution
                var weatherData = SimulateWeatherTool(toolCall.Function.Arguments);
                Console.WriteLine($"  Tool returned: {weatherData}");
                
                // Add tool response to conversation
                messages.Add(new ChatMessage
                {
                    Role = "tool",
                    Content = weatherData,
                    ToolCallId = toolCall.Id
                });
            }
            
            // Step 2: Follow-up request with tool results
            Console.WriteLine("\nSending follow-up request with tool results...");
            
            var followUpRequest = new ChatCompletionRequest
            {
                Model = "llama-3.3-70b",
                Messages = messages,
                Temperature = 0.7
            };
            
            var followUpResponse = await client.Chat.CreateAsync(followUpRequest);
            Console.WriteLine($"\nAssistant: {followUpResponse.Choices[0].Message.Content}");
        }
        else
        {
            Console.WriteLine($"Assistant: {response.Choices[0].Message.Content}");
        }
        
        Console.WriteLine("\n");
    }
    
    private static async Task DemoStreamingWithTools(ICerebrasClientV2 client)
    {
        Console.WriteLine("4. Streaming with Tools Demo");
        Console.WriteLine("----------------------------");
        
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() 
                { 
                    Role = "user", 
                    Content = "Search for the latest AI news and tell me about it." 
                }
            },
            Tools = new List<Tool>
            {
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "search_news",
                        Description = "Search for news articles",
                        Parameters = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["query"] = new Dictionary<string, string>
                                {
                                    ["type"] = "string",
                                    ["description"] = "Search query"
                                },
                                ["category"] = new Dictionary<string, object>
                                {
                                    ["type"] = "string",
                                    ["enum"] = new[] { "technology", "science", "business", "general" }
                                }
                            },
                            ["required"] = new[] { "query" }
                        }
                    }
                }
            },
            Stream = true,
            Temperature = 0.7
        };
        
        Console.Write("Assistant: ");
        
        var toolCalls = new Dictionary<int, (string id, string name, string arguments)>();
        
        await foreach (var chunk in client.Chat.CreateStreamAsync(request))
        {
            if (chunk.Choices[0].Delta?.Content != null)
            {
                Console.Write(chunk.Choices[0].Delta.Content);
            }
            
            if (chunk.Choices[0].Delta?.ToolCalls != null)
            {
                foreach (var toolCall in chunk.Choices[0].Delta.ToolCalls!)
                {
                    var toolId = toolCall.Id;
                    var index = toolCalls.Count;
                    
                    // Find existing tool call by ID
                    foreach (var kvp in toolCalls)
                    {
                        if (kvp.Value.id == toolId)
                        {
                            index = kvp.Key;
                            break;
                        }
                    }
                    
                    if (!toolCalls.ContainsKey(index))
                    {
                        toolCalls[index] = ("", "", "");
                    }
                    
                    var current = toolCalls[index];
                    
                    if (!string.IsNullOrEmpty(toolCall.Id))
                        current.id = toolCall.Id;
                    
                    if (!string.IsNullOrEmpty(toolCall.Function?.Name))
                        current.name = toolCall.Function.Name;
                    
                    if (toolCall.Function?.Arguments != null)
                        current.arguments += toolCall.Function.Arguments;
                    
                    toolCalls[index] = current;
                }
            }
            
            if (chunk.Choices[0].FinishReason == "tool_calls")
            {
                Console.WriteLine("\n\nTool calls detected:");
                foreach (var (index, toolCall) in toolCalls)
                {
                    Console.WriteLine($"  Tool #{index + 1}: {toolCall.name}");
                    Console.WriteLine($"  Arguments: {toolCall.arguments}");
                }
            }
        }
        
        Console.WriteLine("\n");
    }
    
    private static string SimulateWeatherTool(string arguments)
    {
        // Simulate weather API response
        var random = new Random();
        var temp = random.Next(15, 30);
        var conditions = new[] { "sunny", "cloudy", "rainy", "partly cloudy" };
        var condition = conditions[random.Next(conditions.Length)];
        
        return JsonSerializer.Serialize(new
        {
            temperature = temp,
            unit = "celsius",
            condition = condition,
            humidity = random.Next(40, 80),
            wind_speed = random.Next(5, 25)
        });
    }
}