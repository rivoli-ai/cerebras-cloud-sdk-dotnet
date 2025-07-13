using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Chat;
using Cerebras.Cloud.Sdk.Completions;
using Cerebras.Cloud.Sdk.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiComparison
{
    /// <summary>
    /// Example demonstrating the differences between Legacy SDK and Modern SDK
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            // Get API key from environment variable
            var apiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("ERROR: CEREBRAS_API_KEY environment variable is not set.");
                Console.WriteLine("Please set it with: export CEREBRAS_API_KEY=your-api-key");
                return;
            }

            Console.WriteLine("=== Cerebras SDK Comparison Example ===");
            Console.WriteLine("This example shows the differences between Legacy SDK and Modern SDK");
            Console.WriteLine("Note: Both SDKs communicate with the same Cerebras API v1 endpoints\n");

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("CerebrasClient:ApiKey", apiKey),
                    new KeyValuePair<string, string?>("CerebrasClient:BaseUrl", "https://api.cerebras.ai/v1/"),
                    new KeyValuePair<string, string?>("CerebrasClient:DefaultModel", "llama-3.3-70b"),
                    new KeyValuePair<string, string?>("CerebrasClient:DefaultMaxTokens", "100")
                })
                .Build();

            // Build TWO service containers - one for each SDK version
            var legacyServices = new ServiceCollection();
            ConfigureServices(legacyServices, configuration);
            legacyServices.AddCerebrasClient(configuration);
            var legacyProvider = legacyServices.BuildServiceProvider();

            var v2Services = new ServiceCollection();
            ConfigureServices(v2Services, configuration);
            v2Services.AddCerebrasClientV2(configuration);
            var v2Provider = v2Services.BuildServiceProvider();

            try
            {
                // Example 1: Basic Text Completion
                await CompareTextCompletion(legacyProvider, v2Provider);

                // Example 2: Chat Completion (V2 only, with legacy workaround)
                await CompareChatCompletion(legacyProvider, v2Provider);

                // Example 3: Streaming
                await CompareStreaming(legacyProvider, v2Provider);

                // Example 4: Model Listing
                await CompareModelListing(legacyProvider, v2Provider);

                // Example 5: Tool Calling (Modern SDK only)
                await DemoToolCallingModernSdkOnly(v2Provider);

                Console.WriteLine("\n=== All comparisons completed successfully! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise
            });
        }

        private static async Task CompareTextCompletion(IServiceProvider legacyProvider, IServiceProvider v2Provider)
        {
            Console.WriteLine("\n=== 1. Text Completion Comparison ===");
            
            var prompt = "The benefits of cloud computing include:";
            
            // Legacy SDK
            Console.WriteLine("\nLegacy SDK:");
            Console.WriteLine("```csharp");
            Console.WriteLine("var response = await client.GenerateCompletionAsync(new CompletionRequest");
            Console.WriteLine("{");
            Console.WriteLine("    Model = \"llama-3.3-70b\",");
            Console.WriteLine("    Prompt = \"The benefits of cloud computing include:\",");
            Console.WriteLine("    MaxTokens = 50");
            Console.WriteLine("});");
            Console.WriteLine("```");
            
            var legacyClient = legacyProvider.GetRequiredService<ICerebrasClient>();
            var legacyResponse = await legacyClient.GenerateCompletionAsync(new CompletionRequest
            {
                Model = "llama-3.3-70b",
                Prompt = prompt,
                MaxTokens = 50,
                Temperature = 0.7
            });
            
            Console.WriteLine($"Result: {legacyResponse.Text.Trim()}");
            
            // Modern SDK
            Console.WriteLine("\nModern SDK:");
            Console.WriteLine("```csharp");
            Console.WriteLine("var response = await client.Completions.CreateAsync(new TextCompletionRequest");
            Console.WriteLine("{");
            Console.WriteLine("    Model = \"llama-3.3-70b\",");
            Console.WriteLine("    Prompt = \"The benefits of cloud computing include:\",");
            Console.WriteLine("    MaxTokens = 50");
            Console.WriteLine("});");
            Console.WriteLine("```");
            
            var v2Client = v2Provider.GetRequiredService<ICerebrasClientV2>();
            var v2Response = await v2Client.Completions.CreateAsync(new TextCompletionRequest
            {
                Model = "llama-3.3-70b",
                Prompt = prompt,
                MaxTokens = 50,
                Temperature = 0.7
            });
            
            Console.WriteLine($"Result: {v2Response.Choices[0].Text.Trim()}");
        }

        private static async Task CompareChatCompletion(IServiceProvider legacyProvider, IServiceProvider v2Provider)
        {
            Console.WriteLine("\n=== 2. Chat Completion Comparison ===");
            
            var userMessage = "What is the capital of France? Answer in one sentence.";
            
            // Legacy SDK (no native chat support)
            Console.WriteLine("\nLegacy SDK (using completion as workaround):");
            Console.WriteLine("```csharp");
            Console.WriteLine("// Legacy SDK doesn't have native chat support");
            Console.WriteLine("// Must format as a completion prompt");
            Console.WriteLine("var response = await client.GenerateCompletionAsync(new CompletionRequest");
            Console.WriteLine("{");
            Console.WriteLine("    Model = \"llama-3.3-70b\",");
            Console.WriteLine("    Prompt = \"User: What is the capital of France?\\nAssistant:\",");
            Console.WriteLine("    MaxTokens = 50");
            Console.WriteLine("});");
            Console.WriteLine("```");
            
            var legacyClient = legacyProvider.GetRequiredService<ICerebrasClient>();
            var legacyResponse = await legacyClient.GenerateCompletionAsync(new CompletionRequest
            {
                Model = "llama-3.3-70b",
                Prompt = $"User: {userMessage}\nAssistant:",
                MaxTokens = 50,
                Temperature = 0.1
            });
            
            Console.WriteLine($"Result: {legacyResponse.Text.Trim()}");
            
            // Modern SDK
            Console.WriteLine("\nModern SDK (native chat support):");
            Console.WriteLine("```csharp");
            Console.WriteLine("var response = await client.Chat.CreateAsync(new ChatCompletionRequest");
            Console.WriteLine("{");
            Console.WriteLine("    Model = \"llama-3.3-70b\",");
            Console.WriteLine("    Messages = new List<ChatMessage>");
            Console.WriteLine("    {");
            Console.WriteLine("        new() { Role = \"system\", Content = \"You are a helpful assistant.\" },");
            Console.WriteLine("        new() { Role = \"user\", Content = \"What is the capital of France?\" }");
            Console.WriteLine("    },");
            Console.WriteLine("    MaxTokens = 50");
            Console.WriteLine("});");
            Console.WriteLine("```");
            
            var v2Client = v2Provider.GetRequiredService<ICerebrasClientV2>();
            var v2Response = await v2Client.Chat.CreateAsync(new ChatCompletionRequest
            {
                Model = "llama-3.3-70b",
                Messages = new List<ChatMessage>
                {
                    new() { Role = "system", Content = "You are a helpful assistant." },
                    new() { Role = "user", Content = userMessage }
                },
                MaxTokens = 50,
                Temperature = 0.1
            });
            
            Console.WriteLine($"Result: {v2Response.Choices[0].Message.Content}");
        }

        private static async Task CompareStreaming(IServiceProvider legacyProvider, IServiceProvider v2Provider)
        {
            Console.WriteLine("\n=== 3. Streaming Comparison ===");
            
            var prompt = "Count from 1 to 5:";
            
            // Legacy SDK
            Console.WriteLine("\nLegacy SDK streaming:");
            Console.WriteLine("```csharp");
            Console.WriteLine("await foreach (var chunk in client.GenerateCompletionStreamAsync(request))");
            Console.WriteLine("{");
            Console.WriteLine("    Console.Write(chunk.Text);");
            Console.WriteLine("}");
            Console.WriteLine("```");
            
            Console.Write("Result: ");
            var legacyClient = legacyProvider.GetRequiredService<ICerebrasClient>();
            await foreach (var chunk in legacyClient.GenerateCompletionStreamAsync(new CompletionRequest
            {
                Model = "llama-3.3-70b",
                Prompt = prompt,
                MaxTokens = 30,
                Temperature = 0.1
            }))
            {
                Console.Write(chunk.Text);
            }
            Console.WriteLine();
            
            // Modern SDK
            Console.WriteLine("\nModern SDK streaming (with richer metadata):");
            Console.WriteLine("```csharp");
            Console.WriteLine("await foreach (var chunk in client.Chat.CreateStreamAsync(request))");
            Console.WriteLine("{");
            Console.WriteLine("    if (chunk.Choices[0].Delta?.Content != null)");
            Console.WriteLine("        Console.Write(chunk.Choices[0].Delta.Content);");
            Console.WriteLine("}");
            Console.WriteLine("```");
            
            Console.Write("Result: ");
            var v2Client = v2Provider.GetRequiredService<ICerebrasClientV2>();
            await foreach (var chunk in v2Client.Chat.CreateStreamAsync(new ChatCompletionRequest
            {
                Model = "llama-3.3-70b",
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = prompt }
                },
                MaxTokens = 30,
                Temperature = 0.1,
                Stream = true
            }))
            {
                if (chunk.Choices[0].Delta?.Content != null)
                {
                    Console.Write(chunk.Choices[0].Delta.Content);
                }
            }
            Console.WriteLine();
        }

        private static async Task CompareModelListing(IServiceProvider legacyProvider, IServiceProvider v2Provider)
        {
            Console.WriteLine("\n=== 4. Model Listing Comparison ===");
            
            // Legacy SDK
            Console.WriteLine("\nLegacy SDK:");
            Console.WriteLine("```csharp");
            Console.WriteLine("var models = await client.ListModelsAsync();");
            Console.WriteLine("var model = await client.GetModelAsync(\"llama-3.3-70b\");");
            Console.WriteLine("```");
            
            var legacyClient = legacyProvider.GetRequiredService<ICerebrasClient>();
            var legacyModels = await legacyClient.ListModelsAsync();
            Console.WriteLine($"Found {legacyModels.Count} models");
            
            // Modern SDK
            Console.WriteLine("\nModern SDK (service-oriented):");
            Console.WriteLine("```csharp");
            Console.WriteLine("var models = await client.Models.ListAsync();");
            Console.WriteLine("var model = await client.Models.RetrieveAsync(\"llama-3.3-70b\");");
            Console.WriteLine("// Also supports legacy methods for compatibility:");
            Console.WriteLine("var models2 = await client.ListModelsAsync();");
            Console.WriteLine("```");
            
            var v2Client = v2Provider.GetRequiredService<ICerebrasClientV2>();
            var v2Models = await v2Client.Models.ListAsync();
            Console.WriteLine($"Found {v2Models.Count} models via service");
            
            var v2ModelsCompat = await v2Client.ListModelsAsync();
            Console.WriteLine($"Found {v2ModelsCompat.Count} models via compatibility method");
        }

        private static async Task DemoToolCallingModernSdkOnly(IServiceProvider v2Provider)
        {
            Console.WriteLine("\n=== 5. Tool Calling (Modern SDK Only Feature) ===");
            Console.WriteLine("Legacy SDK does not support tool calling.");
            Console.WriteLine("Modern SDK has full support for function/tool calling:\n");
            
            Console.WriteLine("```csharp");
            Console.WriteLine("var request = new ChatCompletionRequest");
            Console.WriteLine("{");
            Console.WriteLine("    Model = \"llama-3.3-70b\",");
            Console.WriteLine("    Messages = new List<ChatMessage> { ... },");
            Console.WriteLine("    Tools = new List<Tool>");
            Console.WriteLine("    {");
            Console.WriteLine("        new() { Type = \"function\", Function = weatherFunction }");
            Console.WriteLine("    },");
            Console.WriteLine("    ToolChoice = \"auto\"");
            Console.WriteLine("};");
            Console.WriteLine("```");
            
            var v2Client = v2Provider.GetRequiredService<ICerebrasClientV2>();
            
            var request = new ChatCompletionRequest
            {
                Model = "llama-3.3-70b",
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "What's 25 multiplied by 4?" }
                },
                Tools = new List<Tool>
                {
                    new()
                    {
                        Type = "function",
                        Function = new FunctionDefinition
                        {
                            Name = "multiply",
                            Description = "Multiply two numbers",
                            Parameters = new Dictionary<string, object>
                            {
                                ["type"] = "object",
                                ["properties"] = new Dictionary<string, object>
                                {
                                    ["a"] = new Dictionary<string, string> { ["type"] = "number" },
                                    ["b"] = new Dictionary<string, string> { ["type"] = "number" }
                                },
                                ["required"] = new[] { "a", "b" }
                            }
                        }
                    }
                },
                Temperature = 0.1
            };
            
            var response = await v2Client.Chat.CreateAsync(request);
            
            if (response.Choices[0].Message.ToolCalls?.Count > 0)
            {
                var toolCall = response.Choices[0].Message.ToolCalls![0];
                Console.WriteLine($"\nModern SDK called tool: {toolCall.Function.Name}");
                Console.WriteLine($"Arguments: {toolCall.Function.Arguments}");
            }
            else
            {
                Console.WriteLine($"\nResponse: {response.Choices[0].Message.Content ?? "No content"}");
            }
        }
    }
}