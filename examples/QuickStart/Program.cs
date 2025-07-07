using System;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

class Program
{
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

        // Configure Cerebras client
        services.Configure<CerebrasClientOptions>(options =>
        {
            // API key can be set here or via CEREBRAS_API_KEY environment variable
            options.ApiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");
            options.DefaultModel = "llama-3.3-70b";
            options.DefaultTemperature = 0.7;
            options.DefaultMaxTokens = 1024;
        });

        // Add HTTP client
        services.AddHttpClient<CerebrasClient>();

        // Register Cerebras client
        services.AddTransient<ICerebrasClient, CerebrasClient>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Get the client
        var client = serviceProvider.GetRequiredService<ICerebrasClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Example 1: Simple completion
            logger.LogInformation("Example 1: Simple completion");
            var response = await client.GenerateCompletionAsync(new CompletionRequest
            {
                Model = "llama-3.3-70b",
                Prompt = "Example 1: What is the capital of France? Answer in one sentence.",
                MaxTokens = 50,
                Temperature = 0.1
            });

            Console.WriteLine($"Response: {response.Text}");
            Console.WriteLine($"Tokens used: {response.Usage?.TotalTokens ?? 0}");
            Console.WriteLine();

            // Example 2: Streaming completion
            logger.LogInformation("Example 2: Streaming completion");
            Console.WriteLine("Streaming response:");
            
            await foreach (var chunk in client.GenerateCompletionStreamAsync(new CompletionRequest
            {
                Model = "llama-3.3-70b",
                Prompt = "Write a haiku about programming.",
                MaxTokens = 100,
                Temperature = 0.7,
                Stream = true
            }))
            {
                Console.Write(chunk.Text);
                if (chunk.IsFinished)
                {
                    Console.WriteLine("\n[Stream completed]");
                }
            }
            Console.WriteLine();

            // Example 3: List available models
            logger.LogInformation("Example 3: Listing available models");
            var models = await client.ListModelsAsync();
            
            Console.WriteLine("Available models:");
            foreach (var model in models)
            {
                Console.WriteLine($"  - {model.Id}: {model.Name} (Context: {model.ContextWindow} tokens)");
            }
        }
        catch (CerebrasApiException ex)
        {
            logger.LogError(ex, "API error occurred");
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.StatusCode.HasValue)
            {
                Console.WriteLine($"Status Code: {ex.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred");
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}