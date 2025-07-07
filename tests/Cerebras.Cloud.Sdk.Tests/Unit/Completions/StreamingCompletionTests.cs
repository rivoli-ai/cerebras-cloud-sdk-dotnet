using System.Collections.Generic;
using System.Text.Json;
using Cerebras.Cloud.Sdk.Completions;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit.Completions;

public class StreamingCompletionTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [Fact]
    public void TextCompletionStreamChoice_Serialization_Works()
    {
        // Arrange
        var choice = new TextCompletionStreamChoice
        {
            Text = "Streaming text",
            Index = 0,
            Logprobs = new LogprobResult
            {
                Tokens = new List<string> { "Stream", "ing" },
                TokenLogprobs = new List<double?> { -0.5, -0.3 },
                TopLogprobs = new List<Dictionary<string, double>?>
                {
                    new Dictionary<string, double> { ["Stream"] = -0.5 },
                    new Dictionary<string, double> { ["ing"] = -0.3 }
                },
                TextOffset = new List<int> { 0, 6 }
            },
            FinishReason = null
        };

        // Act
        var json = JsonSerializer.Serialize(choice, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<TextCompletionStreamChoice>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("Streaming text", deserialized.Text);
        Assert.Equal(0, deserialized.Index);
        Assert.NotNull(deserialized.Logprobs);
        Assert.Equal(2, deserialized.Logprobs.Tokens?.Count);
        Assert.Null(deserialized.FinishReason);
    }

    [Fact]
    public void TextCompletionStreamChoice_WithFinishReason_Works()
    {
        // Arrange
        var choice = new TextCompletionStreamChoice
        {
            Text = "",
            Index = 1,
            Logprobs = null,
            FinishReason = "length"
        };

        // Act
        var json = JsonSerializer.Serialize(choice, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<TextCompletionStreamChoice>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("", deserialized.Text);
        Assert.Equal(1, deserialized.Index);
        Assert.Null(deserialized.Logprobs);
        Assert.Equal("length", deserialized.FinishReason);
    }

    [Fact]
    public void TextCompletionChunk_AllProperties_Work()
    {
        // Arrange
        var chunk = new TextCompletionChunk
        {
            Id = "chunk_123",
            Object = "text_completion.chunk",
            Created = 1234567890,
            Model = "test-model",
            Choices = new List<TextCompletionStreamChoice>
            {
                new()
                {
                    Text = "First chunk",
                    Index = 0,
                    Logprobs = null,
                    FinishReason = null
                },
                new()
                {
                    Text = "Second chunk",
                    Index = 1,
                    Logprobs = null,
                    FinishReason = null
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(chunk, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<TextCompletionChunk>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("chunk_123", deserialized.Id);
        Assert.Equal("text_completion.chunk", deserialized.Object);
        Assert.Equal(1234567890, deserialized.Created);
        Assert.Equal("test-model", deserialized.Model);
        Assert.Equal(2, deserialized.Choices.Count);
        Assert.Equal("First chunk", deserialized.Choices[0].Text);
        Assert.Equal("Second chunk", deserialized.Choices[1].Text);
    }

    [Fact]
    public void TextCompletionChunk_EmptyChoices_Works()
    {
        // Arrange
        var chunk = new TextCompletionChunk
        {
            Id = "empty_chunk",
            Object = "text_completion.chunk",
            Created = 9876543210,
            Model = "model-2",
            Choices = new List<TextCompletionStreamChoice>()
        };

        // Act
        var json = JsonSerializer.Serialize(chunk, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<TextCompletionChunk>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("empty_chunk", deserialized.Id);
        Assert.Empty(deserialized.Choices);
    }

    [Fact]
    public void TextCompletionChoice_WithLogprobs_Serializes()
    {
        // Arrange
        var choice = new TextCompletionChoice
        {
            Text = "Complete text",
            Index = 0,
            Logprobs = new LogprobResult
            {
                Tokens = new List<string> { "Complete", " ", "text" },
                TokenLogprobs = new List<double?> { -0.1, -0.2, -0.15 },
                TopLogprobs = null,
                TextOffset = new List<int> { 0, 8, 9 }
            },
            FinishReason = "stop"
        };

        // Act
        var json = JsonSerializer.Serialize(choice, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<TextCompletionChoice>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("Complete text", deserialized.Text);
        Assert.NotNull(deserialized.Logprobs);
        Assert.Equal(3, deserialized.Logprobs.Tokens?.Count);
        Assert.Equal("stop", deserialized.FinishReason);
    }

    [Fact]
    public void LogprobResult_NullableFields_Work()
    {
        // Arrange
        var logprobs = new LogprobResult
        {
            Tokens = null,
            TokenLogprobs = null,
            TopLogprobs = null,
            TextOffset = null
        };

        // Act
        var json = JsonSerializer.Serialize(logprobs, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<LogprobResult>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Tokens);
        Assert.Null(deserialized.TokenLogprobs);
        Assert.Null(deserialized.TopLogprobs);
        Assert.Null(deserialized.TextOffset);
    }
}