using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Exceptions;
using Cerebras.Cloud.Sdk.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit.Http;

public class HttpServiceTests
{
    private readonly Mock<ILogger<HttpService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly CerebrasClientOptions _options;
    private readonly HttpService _service;

    public HttpServiceTests()
    {
        _loggerMock = new Mock<ILogger<HttpService>>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.cerebras.ai/")
        };
        _options = new CerebrasClientOptions
        {
            BaseUrl = "https://api.cerebras.ai/",
            ApiKey = "test-api-key",
            MaxRetries = 3,
            TimeoutSeconds = 30
        };

        _service = new HttpService(_httpClient, _loggerMock.Object, Options.Create(_options));
    }

    [Fact]
    public async Task SendAsync_Success_ReturnsResponse()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "v1/models");
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":[]}")
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _service.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
    }

    [Fact]
    public async Task SendAsync_AddsHeaders_Correctly()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "v1/models");
        HttpRequestMessage? capturedRequest = null;

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        await _service.SendAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer test-api-key", capturedRequest.Headers.Authorization?.ToString());
        Assert.Contains("Cerebras-DotNet-SDK", capturedRequest.Headers.UserAgent.ToString());
        Assert.True(capturedRequest.Headers.Contains("X-Request-Id"));
    }

    [Fact]
    public async Task SendAsync_ApiError_ThrowsException()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        var errorResponse = new
        {
            error = new
            {
                message = "Invalid API key",
                type = "authentication_error",
                code = "invalid_api_key"
            }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(JsonSerializer.Serialize(errorResponse))
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            async () => await _service.SendAsync(request));

        Assert.Equal("Invalid API key", exception.Message);
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("authentication_error", exception.ErrorType);
        Assert.Equal("invalid_api_key", exception.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_RetriesOnTransientErrors()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "v1/models");
        var callCount = 0;

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var response = await _service.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task SendAsync_DoesNotRetryOnBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        var callCount = 0;

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":{\"message\":\"Invalid request\"}}")
                };
            });

        // Act & Assert
        await Assert.ThrowsAsync<CerebrasApiException>(
            async () => await _service.SendAsync(request));

        Assert.Equal(1, callCount); // Should not retry
    }

    [Fact]
    public async Task SendStreamAsync_Success_ReturnsChunks()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        var streamContent = @"data: {""id"":""1"",""text"":""Hello""}

data: {""id"":""2"",""text"":""World""}

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

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var chunks = await _service.SendStreamAsync<TestChunk>(request, jsonOptions).ToListAsync();

        // Assert
        Assert.Equal(2, chunks.Count);
        Assert.Equal("1", chunks[0].Id);
        Assert.Equal("Hello", chunks[0].Text);
        Assert.Equal("2", chunks[1].Id);
        Assert.Equal("World", chunks[1].Text);
    }

    [Fact]
    public async Task SendStreamAsync_HandlesEmptyLines()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        var streamContent = @"data: {""id"":""1"",""text"":""Hello""}

        

data: {""id"":""2"",""text"":""World""}

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

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var chunks = await _service.SendStreamAsync<TestChunk>(request, jsonOptions).ToListAsync();

        // Assert
        Assert.Equal(2, chunks.Count);
    }

    [Fact]
    public async Task SendStreamAsync_IgnoresInvalidJson()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        var streamContent = @"data: {""id"":""1"",""text"":""Hello""}

data: {invalid json}

data: {""id"":""2"",""text"":""World""}

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

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var chunks = await _service.SendStreamAsync<TestChunk>(request, jsonOptions).ToListAsync();

        // Assert
        Assert.Equal(2, chunks.Count); // Invalid JSON chunk should be ignored
    }

    [Fact]
    public async Task SendAsync_NetworkError_ThrowsException()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "v1/models");

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            async () => await _service.SendAsync(request));

        Assert.Equal("Network error occurred", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task SendAsync_Timeout_ThrowsTaskCanceledException()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "v1/models");

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await _service.SendAsync(request));
    }

    private record TestChunk(string Id, string Text);
}