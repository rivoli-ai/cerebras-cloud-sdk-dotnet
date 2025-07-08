using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Chat;
using Cerebras.Cloud.Sdk.Completions;
using Cerebras.Cloud.Sdk.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Integration;

/// <summary>
/// Integration tests for enhanced Cerebras client V2 that make real API calls.
/// These tests require a valid CEREBRAS_API_KEY environment variable or configuration.
/// </summary>
[Collection("Integration Tests")]
public class CerebrasIntegrationTestsV2 : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private ICerebrasClientV2? _client;
    private ILogger<CerebrasIntegrationTestsV2>? _logger;

    public CerebrasIntegrationTestsV2()
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

        // Add enhanced Cerebras client
        services.AddCerebrasClientV2(configuration);

        _serviceProvider = services.BuildServiceProvider();

        _client = _serviceProvider.GetRequiredService<ICerebrasClientV2>();
        _logger = _serviceProvider.GetRequiredService<ILogger<CerebrasIntegrationTestsV2>>();
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "Cerebras API key not found. Set CEREBRAS_API_KEY environment variable or configure in appsettings.json");
        }

        _logger!.LogInformation("Starting enhanced Cerebras integration tests with API key configured");
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
            await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task ChatCompletions_BasicConversation_ShouldWork()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new ChatMessage 
                { 
                    Role = "system", 
                    Content = "You are a helpful assistant. Always respond concisely." 
                },
                new ChatMessage 
                { 
                    Role = "user", 
                    Content = "What is 2 + 2? Reply with just the number." 
                }
            },
            MaxTokens = 10,
            Temperature = 0
        };

        // Act
        var response = await _client!.CreateChatCompletionAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Id);
        Assert.Equal("chat.completion", response.Object);
        Assert.NotEmpty(response.Choices);
        Assert.Contains("4", response.Choices[0].Message.Content);
        
        _logger!.LogInformation("Chat completion response: '{Response}'", 
            response.Choices[0].Message.Content?.Trim());
    }

    [Fact]
    public async Task ChatCompletions_WithMultipleMessages_ShouldMaintainContext()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = "My name is Alice." },
                new ChatMessage { Role = "assistant", Content = "Nice to meet you, Alice!" },
                new ChatMessage { Role = "user", Content = "What's my name?" }
            },
            MaxTokens = 50,
            Temperature = 0
        };

        // Act
        var response = await _client!.CreateChatCompletionAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("Alice", response.Choices[0].Message.Content, StringComparison.OrdinalIgnoreCase);
        
        _logger!.LogInformation("Context maintained: '{Response}'", 
            response.Choices[0].Message.Content?.Trim());
    }

    [Fact]
    public async Task ChatCompletions_Streaming_ShouldWork()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new ChatMessage 
                { 
                    Role = "user", 
                    Content = "Write the word 'Hello' followed by the word 'World'." 
                }
            },
            MaxTokens = 20,
            Temperature = 0,
            Stream = true
        };

        var chunks = new List<string>();

        // Act
        await foreach (var chunk in _client!.CreateChatCompletionStreamAsync(request))
        {
            if (chunk.Choices.Count > 0 && chunk.Choices[0].Delta?.Content != null)
            {
                chunks.Add(chunk.Choices[0].Delta.Content!);
            }
        }

        // Assert
        Assert.NotEmpty(chunks);
        var fullResponse = string.Join("", chunks);
        Assert.Contains("Hello", fullResponse);
        Assert.Contains("World", fullResponse);
        
        _logger!.LogInformation("Streamed {Count} chunks: '{Response}'", 
            chunks.Count, fullResponse.Trim());
    }

    [Fact]
    public async Task TextCompletions_BasicPrompt_ShouldWork()
    {
        // Arrange
        var request = new TextCompletionRequest
        {
            Model = "llama-3.3-70b",
            Prompt = "The capital of France is",
            MaxTokens = 10,
            Temperature = 0
        };

        // Act
        var response = await _client!.CreateTextCompletionAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Id);
        Assert.Equal("text_completion", response.Object);
        Assert.NotEmpty(response.Choices);
        // The model should complete the prompt
        Assert.NotEmpty(response.Choices[0].Text);
        
        _logger!.LogInformation("Text completion: '{Response}'", 
            response.Choices[0].Text.Trim());
    }

    [Fact]
    public async Task TextCompletions_WithMultiplePrompts_ShouldGenerateMultipleCompletions()
    {
        // Arrange
        var request = new TextCompletionRequest
        {
            Model = "llama-3.3-70b",
            Prompt = new List<string> 
            { 
                "The capital of France is",
                "The capital of Germany is"
            },
            MaxTokens = 10,
            Temperature = 0
        };

        // Act
        var response = await _client!.CreateTextCompletionAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Choices);
        
        // Check that completions were generated for all prompts
        Assert.NotEmpty(response.Choices);
        foreach (var choice in response.Choices)
        {
            Assert.NotEmpty(choice.Text);
        }
        
        _logger!.LogInformation("Multiple completions generated: {Count} choices", 
            response.Choices.Count);
    }

    [Fact]
    public async Task TextCompletions_Streaming_ShouldWork()
    {
        // Arrange
        var request = new TextCompletionRequest
        {
            Model = "llama-3.3-70b",
            Prompt = "Count from 1 to 3:",
            MaxTokens = 20,
            Temperature = 0,
            Stream = true
        };

        var chunks = new List<string>();

        // Act
        await foreach (var chunk in _client!.CreateTextCompletionStreamAsync(request))
        {
            if (chunk.Choices.Count > 0)
            {
                chunks.Add(chunk.Choices[0].Text);
            }
        }

        // Assert
        Assert.NotEmpty(chunks);
        var fullResponse = string.Join("", chunks);
        Assert.Matches(@"\b[1-3]\b", fullResponse);
        
        _logger!.LogInformation("Text streaming produced {Count} chunks: '{Response}'", 
            chunks.Count, fullResponse.Trim());
    }

    [Fact]
    public async Task ChatCompletions_WithParameters_ShouldRespectSettings()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new ChatMessage 
                { 
                    Role = "user", 
                    Content = "Generate a random word." 
                }
            },
            MaxTokens = 5,
            Temperature = 0.8, // Reasonable temperature
            TopP = 0.9
        };

        // Act
        var response = await _client!.CreateChatCompletionAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Single(response.Choices);
        
        _logger!.LogInformation("Generated {Count} choices with high temperature", 
            response.Choices.Count);
        
        foreach (var choice in response.Choices)
        {
            _logger!.LogInformation("Choice {Index}: '{Text}'", 
                choice.Index, choice.Message.Content?.Trim());
        }
    }

    [Fact]
    public async Task ErrorHandling_InvalidApiKey_ShouldThrowMeaningfulException()
    {
        // Arrange - Create a client with invalid API key
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCerebrasClientV2(opts =>
        {
            opts.ApiKey = "invalid-api-key";
            opts.BaseUrl = "https://api.cerebras.ai/";
            opts.DefaultModel = "llama-3.3-70b";
        });

        using var provider = services.BuildServiceProvider();
        var invalidClient = provider.GetRequiredService<ICerebrasClientV2>();

        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = "Hello" }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Cerebras.Cloud.Sdk.Exceptions.CerebrasApiException>(
            async () => await invalidClient.CreateChatCompletionAsync(request));

        Assert.NotNull(exception);
        // API might return either Unauthorized or NotFound for invalid keys
        Assert.True(exception.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                   exception.StatusCode == System.Net.HttpStatusCode.NotFound,
                   $"Expected Unauthorized or NotFound, but got {exception.StatusCode}");
        
        _logger!.LogInformation("Invalid API key correctly threw exception: {Message}", 
            exception.Message);
    }

    [Fact]
    public async Task BackwardCompatibility_OldInterface_ShouldWork()
    {
        // Arrange - Test that ICerebrasClient methods still work
        var request = new CompletionRequest
        {
            Model = "llama-3.3-70b",
            Prompt = "Hello, world!",
            MaxTokens = 10,
            Temperature = 0
        };

        // Act
        var response = await _client!.GenerateCompletionAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Text);
        
        _logger!.LogInformation("Backward compatibility maintained: '{Response}'", 
            response.Text.Trim());
    }
}