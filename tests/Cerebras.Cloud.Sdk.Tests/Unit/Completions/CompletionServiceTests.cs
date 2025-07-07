using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Completions;
using Cerebras.Cloud.Sdk.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit.Completions;

public class CompletionServiceTests
{
    private readonly Mock<IHttpService> _httpServiceMock;
    private readonly Mock<ILogger<CompletionService>> _loggerMock;
    private readonly CompletionService _service;
    private readonly JsonSerializerOptions _jsonOptions;

    public CompletionServiceTests()
    {
        _httpServiceMock = new Mock<IHttpService>();
        _loggerMock = new Mock<ILogger<CompletionService>>();
        _service = new CompletionService(_httpServiceMock.Object, _loggerMock.Object);
        
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
        var request = new TextCompletionRequest
        {
            Model = "llama3.1-70b",
            Prompt = "Once upon a time",
            MaxTokens = 50,
            Temperature = 0.7
        };

        var expectedResponse = new TextCompletionResponse
        {
            Id = "cmpl-123",
            Object = "text_completion",
            Created = 1234567890,
            Model = "llama3.1-70b",
            Choices = new List<TextCompletionChoice>
            {
                new TextCompletionChoice
                {
                    Text = ", there was a magical kingdom...",
                    Index = 0,
                    FinishReason = "stop"
                }
            },
            Usage = new Usage { PromptTokens = 4, CompletionTokens = 8 }
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
        Assert.Equal("cmpl-123", result.Id);
        Assert.Equal("llama3.1-70b", result.Model);
        Assert.Single(result.Choices);
        Assert.Equal(", there was a magical kingdom...", result.Choices[0].Text);
        Assert.Equal(12, result.Usage?.TotalTokens);
    }

    [Fact]
    public async Task CreateAsync_WithArrayPrompt_SendsCorrectRequest()
    {
        // Arrange
        var prompts = new List<string> { "First prompt", "Second prompt" };
        var request = new TextCompletionRequest
        {
            Model = "llama3.1-70b",
            Prompt = prompts,
            MaxTokens = 50
        };

        HttpRequestMessage? capturedRequest = null;
        _httpServiceMock
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new TextCompletionResponse
                {
                    Id = "test",
                    Object = "text_completion",
                    Created = 1234567890,
                    Model = "llama3.1-70b",
                    Choices = new List<TextCompletionChoice>()
                }, _jsonOptions))
            });

        // Act
        await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        var content = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"First prompt\"", content);
        Assert.Contains("\"Second prompt\"", content);
    }

    [Fact]
    public async Task CreateStreamAsync_ValidRequest_ReturnsChunks()
    {
        // Arrange
        var request = new TextCompletionRequest
        {
            Model = "llama3.1-70b",
            Prompt = "Once upon a time",
            Stream = true
        };

        var chunks = new List<TextCompletionChunk>
        {
            new TextCompletionChunk
            {
                Id = "cmpl-123",
                Object = "text_completion.chunk",
                Created = 1234567890,
                Model = "llama3.1-70b",
                Choices = new List<TextCompletionStreamChoice>
                {
                    new TextCompletionStreamChoice
                    {
                        Text = ", there was",
                        Index = 0
                    }
                }
            },
            new TextCompletionChunk
            {
                Id = "cmpl-123",
                Object = "text_completion.chunk",
                Created = 1234567890,
                Model = "llama3.1-70b",
                Choices = new List<TextCompletionStreamChoice>
                {
                    new TextCompletionStreamChoice
                    {
                        Text = " a magical kingdom",
                        Index = 0,
                        FinishReason = "stop"
                    }
                }
            }
        };

        _httpServiceMock
            .Setup(x => x.SendStreamAsync<TextCompletionChunk>(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<JsonSerializerOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        // Act
        var result = await _service.CreateStreamAsync(request).ToListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(", there was", result[0].Choices[0].Text);
        Assert.Equal(" a magical kingdom", result[1].Choices[0].Text);
        Assert.Equal("stop", result[1].Choices[0].FinishReason);
    }

    [Fact]
    public async Task CreateAsync_WithAllParameters_SendsCompleteRequest()
    {
        // Arrange
        var request = new TextCompletionRequest
        {
            Model = "llama3.1-70b",
            Prompt = "Test prompt",
            MaxTokens = 100,
            Temperature = 0.8,
            TopP = 0.95,
            N = 2,
            Logprobs = 5,
            Echo = true,
            Stop = new List<string> { "\n", "END" },
            FrequencyPenalty = 0.6,
            PresencePenalty = 0.4,
            BestOf = 3,
            LogitBias = new Dictionary<string, double> { { "50256", -100 } },
            User = "test-user",
            Suffix = " (continued)",
            Seed = 123
        };

        HttpRequestMessage? capturedRequest = null;
        _httpServiceMock
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new TextCompletionResponse
                {
                    Id = "test",
                    Object = "text_completion",
                    Created = 1234567890,
                    Model = "llama3.1-70b",
                    Choices = new List<TextCompletionChoice>()
                }, _jsonOptions))
            });

        // Act
        await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("completions", capturedRequest.RequestUri?.ToString());
        
        var content = await capturedRequest.Content!.ReadAsStringAsync();
        var requestData = JsonSerializer.Deserialize<JsonDocument>(content, _jsonOptions);
        
        Assert.Equal("llama3.1-70b", requestData?.RootElement.GetProperty("model").GetString());
        Assert.Equal("Test prompt", requestData?.RootElement.GetProperty("prompt").GetString());
        Assert.Equal(100, requestData?.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.Equal(0.8, requestData?.RootElement.GetProperty("temperature").GetDouble());
        Assert.Equal(0.95, requestData?.RootElement.GetProperty("top_p").GetDouble());
        Assert.Equal(2, requestData?.RootElement.GetProperty("n").GetInt32());
        Assert.Equal(5, requestData?.RootElement.GetProperty("logprobs").GetInt32());
        Assert.True(requestData?.RootElement.GetProperty("echo").GetBoolean());
        Assert.Equal(0.6, requestData?.RootElement.GetProperty("frequency_penalty").GetDouble());
        Assert.Equal(0.4, requestData?.RootElement.GetProperty("presence_penalty").GetDouble());
        Assert.Equal(3, requestData?.RootElement.GetProperty("best_of").GetInt32());
        Assert.Equal("test-user", requestData?.RootElement.GetProperty("user").GetString());
        Assert.Equal(" (continued)", requestData?.RootElement.GetProperty("suffix").GetString());
        Assert.Equal(123, requestData?.RootElement.GetProperty("seed").GetInt32());
        Assert.False(requestData?.RootElement.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CreateAsync(null!));
    }
}