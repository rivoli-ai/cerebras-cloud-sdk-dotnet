using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit;

public class CerebrasClientTests
{
    private readonly Mock<ILogger<CerebrasClient>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly CerebrasClientOptions _options;
    private readonly CerebrasClient _client;

    public CerebrasClientTests()
    {
        _loggerMock = new Mock<ILogger<CerebrasClient>>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);
        _options = new CerebrasClientOptions
        {
            BaseUrl = "https://api.cerebras.ai/v1/",
            DefaultModel = "llama3.1-70b",
            DefaultMaxTokens = 1024,
            DefaultTemperature = 0.7,
            ApiKey = "test-api-key"
        };

        _client = new CerebrasClient(
            _httpClient,
            _loggerMock.Object,
            Options.Create(_options));
    }

    [Fact]
    public async Task GenerateCompletionAsync_Success_ReturnsCompletionResponse()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "llama3.1-70b",
            Prompt = "Hello, world!",
            MaxTokens = 100,
            Temperature = 0.7
        };

        var responseContent = new
        {
            id = "cmpl-123",
            @object = "chat.completion",
            created = 1234567890,
            model = "llama3.1-70b",
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        role = "assistant",
                        content = "Hello! How can I help you today?"
                    },
                    index = 0,
                    logprobs = (object?)null,
                    finish_reason = "stop"
                }
            },
            usage = new
            {
                prompt_tokens = 4,
                completion_tokens = 8,
                total_tokens = 12
            }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent), Encoding.UTF8, "application/json")
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GenerateCompletionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cmpl-123", result.Id);
        Assert.Equal("llama3.1-70b", result.Model);
        Assert.Equal("Hello! How can I help you today?", result.Text);
        Assert.NotNull(result.Usage);
        Assert.Equal(4, result.Usage.PromptTokens);
        Assert.Equal(8, result.Usage.CompletionTokens);
    }

    [Fact]
    public async Task GenerateCompletionAsync_MissingApiKey_ThrowsException()
    {
        // Arrange
        var options = new CerebrasClientOptions
        {
            BaseUrl = "https://api.cerebras.ai/v1/",
            DefaultModel = "llama3.1-70b",
            ApiKey = null // No API key
        };

        // Create a new HttpClient with handler for this test
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);

        var client = new CerebrasClient(
            httpClient,
            _loggerMock.Object,
            Options.Create(options));

        var request = new CompletionRequest
        {
            Model = "llama3.1-70b",
            Prompt = "Hello, world!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await client.GenerateCompletionAsync(request));
    }

    [Fact]
    public async Task GenerateCompletionAsync_ApiError_ThrowsException()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "llama3.1-70b",
            Prompt = "Hello, world!"
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\": \"Invalid request\"}", Encoding.UTF8, "application/json")
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            async () => await _client.GenerateCompletionAsync(request));
        
        Assert.Contains("Request failed with status BadRequest", exception.Message);
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task ListModelsAsync_Success_ReturnsModelList()
    {
        // Arrange
        var responseContent = new
        {
            @object = "list",
            data = new[]
            {
                new
                {
                    id = "llama3.1-8b",
                    @object = "model",
                    created = 1234567890,
                    owned_by = "cerebras",
                    name = "Llama 3.1 8B",
                    description = "Llama 3.1 8B parameter model",
                    context_length = 8192,
                    is_available = true
                },
                new
                {
                    id = "llama3.1-70b",
                    @object = "model",
                    created = 1234567890,
                    owned_by = "cerebras",
                    name = "Llama 3.1 70B",
                    description = "Llama 3.1 70B parameter model",
                    context_length = 8192,
                    is_available = true
                }
            }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent), Encoding.UTF8, "application/json")
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.ListModelsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("llama3.1-8b", result[0].Id);
        Assert.Equal("Llama 3.1 8B", result[0].Name);
        Assert.Equal("llama3.1-70b", result[1].Id);
        Assert.Equal("Llama 3.1 70B", result[1].Name);
    }

    [Fact]
    public async Task GetModelAsync_Success_ReturnsModel()
    {
        // Arrange
        var modelId = "llama3.1-70b";

        var responseContent = new
        {
            @object = "list",
            data = new[]
            {
                new
                {
                    id = "llama3.1-70b",
                    @object = "model",
                    created = 1234567890,
                    owned_by = "cerebras",
                    name = "Llama 3.1 70B",
                    description = "Llama 3.1 70B parameter model",
                    context_length = 8192,
                    is_available = true
                }
            }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent), Encoding.UTF8, "application/json")
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetModelAsync(modelId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(modelId, result.Id);
        Assert.Equal("Llama 3.1 70B", result.Name);
    }

    [Fact]
    public async Task GetModelAsync_NotFound_ThrowsException()
    {
        // Arrange
        var modelId = "non-existent-model";

        var responseContent = new
        {
            @object = "list",
            data = Array.Empty<object>()
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseContent), Encoding.UTF8, "application/json")
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            async () => await _client.GetModelAsync(modelId));
        
        Assert.Contains($"Model '{modelId}' not found", exception.Message);
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GenerateCompletionStreamAsync_Success_ReturnsChunks()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "llama3.1-70b",
            Prompt = "Hello, world!",
            Stream = true
        };

        var streamContent = @"data: {""id"":""cmpl-123"",""object"":""chat.completion.chunk"",""created"":1234567890,""model"":""llama3.1-70b"",""choices"":[{""delta"":{""content"":""Hello""},""index"":0,""finish_reason"":null}]}

data: {""id"":""cmpl-123"",""object"":""chat.completion.chunk"",""created"":1234567890,""model"":""llama3.1-70b"",""choices"":[{""delta"":{""content"":"" world!""},""index"":0,""finish_reason"":null}]}

data: [DONE]
";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(streamContent, Encoding.UTF8, "text/event-stream")
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var chunks = await _client.GenerateCompletionStreamAsync(request).ToListAsync();

        // Assert
        Assert.Equal(3, chunks.Count);
        Assert.Equal("Hello", chunks[0].Text);
        Assert.False(chunks[0].IsFinished);
        Assert.Equal(" world!", chunks[1].Text);
        Assert.False(chunks[1].IsFinished);
        Assert.Equal("", chunks[2].Text);
        Assert.True(chunks[2].IsFinished);
    }
}