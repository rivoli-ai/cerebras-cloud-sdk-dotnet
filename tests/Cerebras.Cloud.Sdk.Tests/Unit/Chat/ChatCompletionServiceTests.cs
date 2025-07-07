using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Chat;
using Cerebras.Cloud.Sdk.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit.Chat;

public class ChatCompletionServiceTests
{
    private readonly Mock<IHttpService> _httpServiceMock;
    private readonly Mock<ILogger<ChatCompletionService>> _loggerMock;
    private readonly ChatCompletionService _service;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChatCompletionServiceTests()
    {
        _httpServiceMock = new Mock<IHttpService>();
        _loggerMock = new Mock<ILogger<ChatCompletionService>>();
        _service = new ChatCompletionService(_httpServiceMock.Object, _loggerMock.Object);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "llama3.1-70b",
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = "Hello!" }
            },
            MaxTokens = 100,
            Temperature = 0.7
        };

        var expectedResponse = new ChatCompletionResponse
        {
            Id = "chatcmpl-123",
            Object = "chat.completion",
            Created = 1234567890,
            Model = "llama3.1-70b",
            Choices = new List<ChatChoice>
            {
                new ChatChoice
                {
                    Index = 0,
                    Message = new ChatMessage { Role = "assistant", Content = "Hello! How can I help you?" },
                    FinishReason = "stop"
                }
            },
            Usage = new Usage { PromptTokens = 5, CompletionTokens = 10 }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedResponse, _jsonOptions))
        };

        _httpServiceMock
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("chatcmpl-123", result.Id);
        Assert.Equal("llama3.1-70b", result.Model);
        Assert.Single(result.Choices);
        Assert.Equal("Hello! How can I help you?", result.Choices[0].Message.Content);
        Assert.Equal(15, result.Usage?.TotalTokens);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateStreamAsync_ValidRequest_ReturnsChunks()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "llama3.1-70b",
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = "Hello!" }
            },
            Stream = true
        };

        var chunks = new List<ChatCompletionChunk>
        {
            new ChatCompletionChunk
            {
                Id = "chatcmpl-123",
                Object = "chat.completion.chunk",
                Created = 1234567890,
                Model = "llama3.1-70b",
                Choices = new List<ChatStreamChoice>
                {
                    new ChatStreamChoice
                    {
                        Index = 0,
                        Delta = new ChatMessageDelta { Content = "Hello" }
                    }
                }
            },
            new ChatCompletionChunk
            {
                Id = "chatcmpl-123",
                Object = "chat.completion.chunk",
                Created = 1234567890,
                Model = "llama3.1-70b",
                Choices = new List<ChatStreamChoice>
                {
                    new ChatStreamChoice
                    {
                        Index = 0,
                        Delta = new ChatMessageDelta { Content = " there!" },
                        FinishReason = "stop"
                    }
                }
            }
        };

        _httpServiceMock
            .Setup(x => x.SendStreamAsync<ChatCompletionChunk>(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<JsonSerializerOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        // Act
        var result = await _service.CreateStreamAsync(request).ToListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Hello", result[0].Choices[0].Delta.Content);
        Assert.Equal(" there!", result[1].Choices[0].Delta.Content);
        Assert.Equal("stop", result[1].Choices[0].FinishReason);
    }

    [Fact]
    public async Task CreateAsync_WithAllParameters_SendsCompleteRequest()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "llama3.1-70b",
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "system", Content = "You are helpful." },
                new ChatMessage { Role = "user", Content = "Hello!" }
            },
            MaxTokens = 150,
            Temperature = 0.8,
            TopP = 0.9,
            FrequencyPenalty = 0.5,
            PresencePenalty = 0.3,
            Seed = 42,
            N = 2,
            Stop = new List<string> { "\n", "." },
            ResponseFormat = new ResponseFormat { Type = "json_object" },
            User = "test-user",
            Tools = new List<Tool>
            {
                new Tool
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "get_weather",
                        Description = "Get weather info",
                        Parameters = new { type = "object" }
                    }
                }
            }
        };

        HttpRequestMessage? capturedRequest = null;
        _httpServiceMock
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new ChatCompletionResponse
                {
                    Id = "test",
                    Object = "chat.completion",
                    Created = 1234567890,
                    Model = "llama3.1-70b",
                    Choices = new List<ChatChoice>()
                }, _jsonOptions))
            });

        // Act
        await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("chat/completions", capturedRequest.RequestUri?.ToString());
        
        var content = await capturedRequest.Content!.ReadAsStringAsync();
        var requestData = JsonSerializer.Deserialize<JsonDocument>(content, _jsonOptions);
        
        Assert.Equal("llama3.1-70b", requestData?.RootElement.GetProperty("model").GetString());
        Assert.Equal(2, requestData?.RootElement.GetProperty("messages").GetArrayLength());
        Assert.Equal(150, requestData?.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.Equal(0.8, requestData?.RootElement.GetProperty("temperature").GetDouble());
        Assert.Equal(0.9, requestData?.RootElement.GetProperty("top_p").GetDouble());
        Assert.Equal(0.5, requestData?.RootElement.GetProperty("frequency_penalty").GetDouble());
        Assert.Equal(0.3, requestData?.RootElement.GetProperty("presence_penalty").GetDouble());
        Assert.Equal(42, requestData?.RootElement.GetProperty("seed").GetInt32());
        Assert.Equal(2, requestData?.RootElement.GetProperty("n").GetInt32());
        Assert.False(requestData?.RootElement.GetProperty("stream").GetBoolean());
    }
}

// Extension to convert IList to IAsyncEnumerable for testing
internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield();
        }
    }
}