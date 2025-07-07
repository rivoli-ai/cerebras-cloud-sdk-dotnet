using System.Collections.Generic;
using System.Text.Json;
using Cerebras.Cloud.Sdk.Chat;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit.Chat;

public class StreamingModelsTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [Fact]
    public void ChatCompletionChunk_Serialization_Works()
    {
        // Arrange
        var chunk = new ChatCompletionChunk
        {
            Id = "chat-123",
            Object = "chat.completion.chunk",
            Created = 1234567890,
            Model = "llama-3.3-70b",
            Choices = new List<ChatStreamChoice>
            {
                new()
                {
                    Index = 0,
                    Delta = new ChatMessageDelta
                    {
                        Role = "assistant",
                        Content = "Hello"
                    },
                    FinishReason = null
                }
            },
            SystemFingerprint = "fp_123"
        };

        // Act
        var json = JsonSerializer.Serialize(chunk, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ChatCompletionChunk>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(chunk.Id, deserialized.Id);
        Assert.Equal(chunk.Object, deserialized.Object);
        Assert.Equal(chunk.Created, deserialized.Created);
        Assert.Equal(chunk.Model, deserialized.Model);
        Assert.Equal(chunk.SystemFingerprint, deserialized.SystemFingerprint);
        Assert.Single(deserialized.Choices);
        Assert.Equal(0, deserialized.Choices[0].Index);
        Assert.Equal("assistant", deserialized.Choices[0].Delta.Role);
        Assert.Equal("Hello", deserialized.Choices[0].Delta.Content);
    }

    [Fact]
    public void ChatStreamChoice_WithFinishReason_Works()
    {
        // Arrange
        var choice = new ChatStreamChoice
        {
            Index = 1,
            Delta = new ChatMessageDelta
            {
                Content = null
            },
            FinishReason = "stop",
            Logprobs = new { tokens = new[] { "test" } }
        };

        // Act
        var json = JsonSerializer.Serialize(choice, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ChatStreamChoice>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(1, deserialized.Index);
        Assert.Null(deserialized.Delta.Content);
        Assert.Equal("stop", deserialized.FinishReason);
        Assert.NotNull(deserialized.Logprobs);
    }

    [Fact]
    public void ChatMessageDelta_WithToolCalls_Works()
    {
        // Arrange
        var delta = new ChatMessageDelta
        {
            Role = null,
            Content = null,
            ToolCalls = new List<ToolCall>
            {
                new()
                {
                    Id = "call_123",
                    Type = "function",
                    Function = new FunctionCall
                    {
                        Name = "get_weather",
                        Arguments = "{\"location\":\"NYC\"}"
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(delta, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ChatMessageDelta>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Role);
        Assert.Null(deserialized.Content);
        Assert.NotNull(deserialized.ToolCalls);
        Assert.Single(deserialized.ToolCalls);
        Assert.Equal("call_123", deserialized.ToolCalls[0].Id);
        Assert.Equal("function", deserialized.ToolCalls[0].Type);
        Assert.Equal("get_weather", deserialized.ToolCalls[0].Function.Name);
    }

    [Fact]
    public void ChatCompletionChunk_MinimalData_Works()
    {
        // Arrange
        var json = @"{
            ""id"": ""chunk-456"",
            ""object"": ""chat.completion.chunk"",
            ""created"": 9876543210,
            ""model"": ""test-model"",
            ""choices"": []
        }";

        // Act
        var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(json, _jsonOptions);

        // Assert
        Assert.NotNull(chunk);
        Assert.Equal("chunk-456", chunk.Id);
        Assert.Equal("chat.completion.chunk", chunk.Object);
        Assert.Equal(9876543210, chunk.Created);
        Assert.Equal("test-model", chunk.Model);
        Assert.Empty(chunk.Choices);
        Assert.Null(chunk.SystemFingerprint);
    }

    [Fact]
    public void ChatStreamChoice_AllProperties_Serialize()
    {
        // Arrange
        var choice = new ChatStreamChoice
        {
            Index = 2,
            Delta = new ChatMessageDelta
            {
                Role = "system",
                Content = "System message",
                ToolCalls = null
            },
            FinishReason = "length",
            Logprobs = null
        };

        // Act
        var json = JsonSerializer.Serialize(choice, _jsonOptions);
        
        // Assert
        Assert.Contains("\"index\":2", json);
        Assert.Contains("\"delta\":", json);
        Assert.Contains("\"role\":\"system\"", json);
        Assert.Contains("\"content\":\"System message\"", json);
        Assert.Contains("\"finish_reason\":\"length\"", json);
    }
}