using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Integration;

/// <summary>
/// Integration tests for Cerebras client that make real API calls.
/// These tests require a valid CEREBRAS_API_KEY environment variable or configuration.
/// </summary>
[Collection("Integration Tests")]
public class CerebrasIntegrationTests : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private ICerebrasClient? _client;
    private ILogger<CerebrasIntegrationTests>? _logger;

    public CerebrasIntegrationTests()
    {
        // Initialization moved to InitializeAsync
    }

    public async Task InitializeAsync()
    {
        // Get API key from environment
        var apiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");
        
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "CerebrasClient:ApiKey", apiKey },
                { "CerebrasClient:BaseUrl", "https://api.cerebras.ai/v1/" },
                { "CerebrasClient:DefaultModel", "llama-3.3-70b" }
            })
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        // Build service container
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole()
                   .SetMinimumLevel(LogLevel.Information);
        });

        // Add Cerebras client
        services.AddCerebrasClient(configuration);

        _serviceProvider = services.BuildServiceProvider();

        _client = _serviceProvider.GetRequiredService<ICerebrasClient>();
        _logger = _serviceProvider.GetRequiredService<ILogger<CerebrasIntegrationTests>>();
        
        _logger!.LogInformation("Starting Cerebras integration tests");
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
            await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task ListModelsAsync_ShouldReturnAvailableModels()
    {
        // Act
        var models = await _client!.ListModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.NotEmpty(models);

        _logger!.LogInformation("Retrieved {Count} models from Cerebras API", models.Count);

        // Log all available models
        foreach (var model in models)
        {
            _logger!.LogInformation("Model: {Id} - {Name} (Available: {IsAvailable})",
                model.Id, model.Name, model.IsAvailable);
        }

        // Verify at least one model contains "llama"
        Assert.Contains(models, m => m.Id.Contains("llama", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetModelAsync_WithValidModel_ShouldReturnModelDetails()
    {
        // Arrange - First get available models
        var models = await _client!.ListModelsAsync();
        Assert.NotEmpty(models);
        var firstModel = models.First();

        // Act
        var model = await _client!.GetModelAsync(firstModel.Id);

        // Assert
        Assert.NotNull(model);
        Assert.Equal(firstModel.Id, model.Id);
        Assert.NotEmpty(model.Name);

        _logger!.LogInformation("Retrieved model details: {Model}", model.Id);
    }

    [Fact]
    public async Task GetModelAsync_WithInvalidModel_ShouldThrowException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            async () => await _client!.GetModelAsync("non-existent-model-12345"));

        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
        _logger!.LogInformation("Correctly threw exception for invalid model");
    }

    [Fact]
    public async Task GenerateCompletionAsync_WithSimplePrompt_ShouldReturnCompletion()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "llama-3.3-70b",
            Prompt = "What is the capital of France? Answer in one word.",
            MaxTokens = 10,
            Temperature = 0.1 // Low temperature for consistent results
        };

        // Act
        var response = await _client!.GenerateCompletionAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Text);
        Assert.NotEmpty(response.Id);
        Assert.NotEmpty(response.Model);
        Assert.NotNull(response.Usage);
        Assert.True(response.Usage.PromptTokens > 0);
        Assert.True(response.Usage.CompletionTokens > 0);
        Assert.True(response.Usage.TotalTokens > 0);

        _logger!.LogInformation("Generated completion: '{Text}' (Tokens: {Total})",
            response.Text.Trim(), response.Usage.TotalTokens);

        // Verify the response makes sense (should contain Paris)
        Assert.Contains("Paris", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateCompletionAsync_WithInvalidModel_ShouldThrowException()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "invalid-model-name",
            Prompt = "Hello",
            MaxTokens = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            async () => await _client!.GenerateCompletionAsync(request));

        Assert.NotNull(exception);
        _logger!.LogInformation("Correctly threw exception for invalid model");
    }

    [Fact]
    public async Task GenerateCompletionStreamAsync_WithSimplePrompt_ShouldStreamResponse()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "llama-3.3-70b",
            Prompt = "Count from 1 to 5, one number per line.",
            MaxTokens = 50,
            Temperature = 0.1
        };

        var chunks = new List<string>();
        var completedChunks = 0;

        // Act
        await foreach (var chunk in _client!.GenerateCompletionStreamAsync(request))
        {
            chunks.Add(chunk.Text);
            if (chunk.IsFinished)
            {
                completedChunks++;
            }
        }

        // Assert
        Assert.NotEmpty(chunks);
        Assert.True(completedChunks > 0);

        var fullResponse = string.Join("", chunks);
        _logger!.LogInformation("Streamed completion: '{Response}' ({ChunkCount} chunks)",
            fullResponse.Trim(), chunks.Count);

        // Verify the response contains numbers
        Assert.Matches("[1-5]", fullResponse);
    }
}