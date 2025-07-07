using System;
using System.Text.Json;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit.Models;

public class SharedModelsTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [Fact]
    public void Usage_TotalTokens_CalculatedCorrectly()
    {
        // Arrange & Act
        var usage = new Usage
        {
            PromptTokens = 100,
            CompletionTokens = 50
        };

        // Assert
        Assert.Equal(150, usage.TotalTokens);
    }

    [Fact]
    public void Usage_Serialization_Works()
    {
        // Arrange
        var usage = new Usage
        {
            PromptTokens = 25,
            CompletionTokens = 75
        };

        // Act
        var json = JsonSerializer.Serialize(usage, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<Usage>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(25, deserialized.PromptTokens);
        Assert.Equal(75, deserialized.CompletionTokens);
        Assert.Equal(100, deserialized.TotalTokens);
        Assert.Contains("\"prompt_tokens\":25", json);
        Assert.Contains("\"completion_tokens\":75", json);
    }

    [Fact]
    public void CompletionChunk_AllProperties_Work()
    {
        // Arrange
        var chunk = new CompletionChunk
        {
            Text = "Hello, world!",
            IsFinished = false,
            FinishReason = null
        };

        // Act
        var json = JsonSerializer.Serialize(chunk, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<CompletionChunk>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("Hello, world!", deserialized.Text);
        Assert.False(deserialized.IsFinished);
        Assert.Null(deserialized.FinishReason);
    }

    [Fact]
    public void CompletionChunk_Finished_WithReason()
    {
        // Arrange
        var chunk = new CompletionChunk
        {
            Text = "",
            IsFinished = true,
            FinishReason = "stop"
        };

        // Act
        var json = JsonSerializer.Serialize(chunk, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<CompletionChunk>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("", deserialized.Text);
        Assert.True(deserialized.IsFinished);
        Assert.Equal("stop", deserialized.FinishReason);
    }

    [Fact]
    public void Model_AllProperties_Serialize()
    {
        // Arrange
        var model = new Model
        {
            Id = "llama-3.3-70b",
            Name = "Llama 3.3 70B",
            Description = "A large language model",
            ContextWindow = 131072,
            IsAvailable = true
        };

        // Act
        var json = JsonSerializer.Serialize(model, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<Model>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("llama-3.3-70b", deserialized.Id);
        Assert.Equal("Llama 3.3 70B", deserialized.Name);
        Assert.Equal("A large language model", deserialized.Description);
        Assert.Equal(131072, deserialized.ContextWindow);
        Assert.True(deserialized.IsAvailable);
    }

    [Fact]
    public void Model_MinimalProperties_Work()
    {
        // Arrange
        var model = new Model
        {
            Id = "test-model",
            Name = "Test Model",
            Description = null,
            ContextWindow = null,
            IsAvailable = false
        };

        // Act
        var json = JsonSerializer.Serialize(model, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<Model>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("test-model", deserialized.Id);
        Assert.Equal("Test Model", deserialized.Name);
        Assert.Null(deserialized.Description);
        Assert.Null(deserialized.ContextWindow);
        Assert.False(deserialized.IsAvailable);
    }

    [Fact]
    public void CompletionResponse_WithUsage_Works()
    {
        // Arrange
        var response = new CompletionResponse
        {
            Id = "comp_123",
            Model = "llama-3.3-70b",
            Text = "Generated text",
            FinishReason = "stop",
            Usage = new Usage
            {
                PromptTokens = 10,
                CompletionTokens = 20
            },
            CreatedAt = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero)
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<CompletionResponse>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("comp_123", deserialized.Id);
        Assert.Equal("llama-3.3-70b", deserialized.Model);
        Assert.Equal("Generated text", deserialized.Text);
        Assert.Equal("stop", deserialized.FinishReason);
        Assert.NotNull(deserialized.Usage);
        Assert.Equal(30, deserialized.Usage.TotalTokens);
        Assert.Equal(2024, deserialized.CreatedAt.Year);
    }

    [Fact]
    public void CompletionRequest_AllProperties_Serialize()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "test-model",
            Prompt = "Test prompt",
            MaxTokens = 100,
            Temperature = 0.7,
            TopP = 0.9,
            Seed = 42,
            Stream = true
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<CompletionRequest>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("test-model", deserialized.Model);
        Assert.Equal("Test prompt", deserialized.Prompt);
        Assert.Equal(100, deserialized.MaxTokens);
        Assert.Equal(0.7, deserialized.Temperature);
        Assert.Equal(0.9, deserialized.TopP);
        Assert.Equal(42, deserialized.Seed);
        Assert.True(deserialized.Stream);
    }
}