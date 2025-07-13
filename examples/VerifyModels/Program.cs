using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VerifyModels
{
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

            // Determine which SDK version to use
            bool useModernSdk = args.Length > 0 && args[0] == "--v2";
            
            Console.WriteLine($"=== Cerebras Model Endpoint Verification ===");
            Console.WriteLine($"Using {(useModernSdk ? "Modern" : "Legacy")} SDK (ICerebrasClient{(useModernSdk ? "V2" : "")})");
            Console.WriteLine("(Use --v2 flag to switch between SDK versions)\n");
            Console.WriteLine("Note: Both SDKs use the same Cerebras API v1 endpoints");

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("CerebrasClient:ApiKey", apiKey),
                    new KeyValuePair<string, string?>("CerebrasClient:BaseUrl", "https://api.cerebras.ai/v1/")
                })
                .Build();

            // Build service container
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add appropriate Cerebras client based on selection
            if (useModernSdk)
            {
                services.AddCerebrasClientV2(configuration);
            }
            else
            {
                services.AddCerebrasClient(configuration);
            }

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                if (useModernSdk)
                {
                    await VerifyWithModernSdk(serviceProvider, logger);
                }
                else
                {
                    await VerifyWithLegacySdk(serviceProvider, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to verify model endpoint");
                Environment.Exit(1);
            }
        }
        
        private static async Task VerifyWithLegacySdk(IServiceProvider serviceProvider, ILogger<Program> logger)
        {
            var client = serviceProvider.GetRequiredService<ICerebrasClient>();
            
            logger.LogInformation("Verifying Cerebras model endpoint using Legacy SDK (ICerebrasClient)...");
            logger.LogInformation("API Endpoint: https://api.cerebras.ai/v1/models");
            
            // List all available models
            logger.LogInformation("\n=== Listing all available models ===");
            var models = await client.ListModelsAsync();
            
            if (models == null || models.Count == 0)
            {
                logger.LogWarning("No models returned from the API");
                return;
            }
            
            logger.LogInformation("Successfully retrieved {Count} models", models.Count);
            
            // Display each model
            foreach (var model in models)
            {
                logger.LogInformation("Model ID: {Id}", model.Id);
                logger.LogInformation("  Name: {Name}", model.Name);
                logger.LogInformation("  Available: {Available}", model.IsAvailable);
                if (!string.IsNullOrEmpty(model.Description))
                {
                    logger.LogInformation("  Description: {Description}", model.Description);
                }
                if (model.ContextWindow > 0)
                {
                    logger.LogInformation("  Context Window: {ContextWindow} tokens", model.ContextWindow);
                }
                logger.LogInformation(""); // Empty line for readability
            }
            
            // Try to get details for a specific model
            if (models.Count > 0)
            {
                var firstModel = models[0];
                logger.LogInformation("\n=== Getting details for specific model: {ModelId} ===", firstModel.Id);
                
                var modelDetails = await client.GetModelAsync(firstModel.Id);
                
                logger.LogInformation("Successfully retrieved model details:");
                logger.LogInformation("  ID: {Id}", modelDetails.Id);
                logger.LogInformation("  Name: {Name}", modelDetails.Name);
                logger.LogInformation("  Available: {Available}", modelDetails.IsAvailable);
                if (modelDetails.ContextWindow > 0)
                {
                    logger.LogInformation("  Context Window: {ContextWindow} tokens", modelDetails.ContextWindow);
                }
            }
            
            logger.LogInformation("\n✅ Model endpoint verification completed successfully using Legacy SDK!");
        }
        
        private static async Task VerifyWithModernSdk(IServiceProvider serviceProvider, ILogger<Program> logger)
        {
            var client = serviceProvider.GetRequiredService<ICerebrasClientV2>();
            
            logger.LogInformation("Verifying Cerebras model endpoint using Modern SDK (ICerebrasClientV2)...");
            logger.LogInformation("API Endpoint: https://api.cerebras.ai/v1/models");
            
            // List all available models using modern SDK service
            logger.LogInformation("\n=== Listing all available models using Models Service ===");
            var models = await client.Models.ListAsync();
            
            if (models == null || models.Count == 0)
            {
                logger.LogWarning("No models returned from the API");
                return;
            }
            
            logger.LogInformation("Successfully retrieved {Count} models", models.Count);
            
            // Display each model
            foreach (var model in models)
            {
                logger.LogInformation("Model ID: {Id}", model.Id);
                logger.LogInformation("  Name: {Name}", model.Name);
                logger.LogInformation("  Available: {Available}", model.IsAvailable);
                if (!string.IsNullOrEmpty(model.Description))
                {
                    logger.LogInformation("  Description: {Description}", model.Description);
                }
                if (model.ContextWindow > 0)
                {
                    logger.LogInformation("  Context Window: {ContextWindow} tokens", model.ContextWindow);
                }
                logger.LogInformation(""); // Empty line for readability
            }
            
            // Try to get details for a specific model using service method
            if (models.Count > 0)
            {
                var firstModel = models[0];
                logger.LogInformation("\n=== Getting details for specific model using Models.RetrieveAsync: {ModelId} ===", firstModel.Id);
                
                var modelDetails = await client.Models.RetrieveAsync(firstModel.Id);
                
                logger.LogInformation("Successfully retrieved model details:");
                logger.LogInformation("  ID: {Id}", modelDetails.Id);
                logger.LogInformation("  Name: {Name}", modelDetails.Name);
                logger.LogInformation("  Available: {Available}", modelDetails.IsAvailable);
                if (modelDetails.ContextWindow > 0)
                {
                    logger.LogInformation("  Context Window: {ContextWindow} tokens", modelDetails.ContextWindow);
                }
            }
            
            // Also demonstrate modern SDK backward compatibility methods
            logger.LogInformation("\n=== Testing backward compatibility methods ===");
            var modelsViaConvenience = await client.ListModelsAsync();
            logger.LogInformation("Retrieved {Count} models via legacy-style method (ListModelsAsync)", modelsViaConvenience.Count);
            
            if (modelsViaConvenience.Count > 0)
            {
                var modelViaConvenience = await client.GetModelAsync(modelsViaConvenience[0].Id);
                logger.LogInformation("Retrieved model '{Id}' via legacy-style method (GetModelAsync)", modelViaConvenience.Id);
            }
            
            logger.LogInformation("\n✅ Model endpoint verification completed successfully using Modern SDK!");
            logger.LogInformation("Note: Both SDKs communicate with the same API endpoints.");
        }
    }
}