using System;
using System.Net;
using System.Net.Http;
using System.Text;
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

public class HttpServiceErrorTests
{
    private readonly Mock<ILogger<HttpService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly IOptions<CerebrasClientOptions> _options;

    public HttpServiceErrorTests()
    {
        _mockLogger = new Mock<ILogger<HttpService>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object)
        {
            BaseAddress = new Uri("https://api.test.com/")
        };
        
        _options = Options.Create(new CerebrasClientOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.test.com/",
            TimeoutSeconds = 30
        });
    }

    [Fact]
    public async Task SendAsync_ServerError_ThrowsCerebrasApiException()
    {
        // Arrange
        var errorResponse = @"{""error"":{""message"":""Internal server error"",""type"":""server_error""}}";
        
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(errorResponse, Encoding.UTF8, "application/json")
            });

        var service = new HttpService(_httpClient, _mockLogger.Object, _options);
        var request = new HttpRequestMessage(HttpMethod.Post, "test");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            () => service.SendAsync(request, CancellationToken.None));
        
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("Internal server error", exception.Message);
    }

    [Fact]
    public async Task SendAsync_Unauthorized_ThrowsCerebrasApiException()
    {
        // Arrange
        var errorResponse = @"{""error"":{""message"":""Invalid API key"",""type"":""authentication_error""}}";
        
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(errorResponse, Encoding.UTF8, "application/json")
            });

        var service = new HttpService(_httpClient, _mockLogger.Object, _options);
        var request = new HttpRequestMessage(HttpMethod.Get, "models");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            () => service.SendAsync(request, CancellationToken.None));
        
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Contains("Invalid API key", exception.Message);
    }

    [Fact]
    public async Task SendAsync_RateLimited_ThrowsCerebrasApiException()
    {
        // Arrange
        var errorResponse = @"{""error"":{""message"":""Rate limit exceeded"",""type"":""rate_limit_error""}}";
        
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent(errorResponse, Encoding.UTF8, "application/json")
            });

        var service = new HttpService(_httpClient, _mockLogger.Object, _options);
        var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            () => service.SendAsync(request, CancellationToken.None));
        
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Contains("Rate limit exceeded", exception.Message);
    }

    [Fact]
    public async Task SendAsync_InvalidJson_ThrowsCerebrasApiException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(invalidJson, Encoding.UTF8, "application/json")
            });

        var service = new HttpService(_httpClient, _mockLogger.Object, _options);
        var request = new HttpRequestMessage(HttpMethod.Post, "test");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            () => service.SendAsync(request, CancellationToken.None));
        
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        // Should fall back to the raw content when JSON parsing fails
        Assert.Contains("invalid json", exception.Message);
    }

    [Fact]
    public async Task SendAsync_EmptyErrorResponse_ThrowsCerebrasApiException()
    {
        // Arrange
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("", Encoding.UTF8, "application/json")
            });

        var service = new HttpService(_httpClient, _mockLogger.Object, _options);
        var request = new HttpRequestMessage(HttpMethod.Post, "test");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            () => service.SendAsync(request, CancellationToken.None));
        
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_NetworkError_ThrowsCerebrasApiException()
    {
        // Arrange
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = new HttpService(_httpClient, _mockLogger.Object, _options);
        var request = new HttpRequestMessage(HttpMethod.Get, "test");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CerebrasApiException>(
            () => service.SendAsync(request, CancellationToken.None));
        
        Assert.Contains("Network error", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task SendAsync_Timeout_ThrowsTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var service = new HttpService(_httpClient, _mockLogger.Object, _options);
        var request = new HttpRequestMessage(HttpMethod.Post, "test");

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => service.SendAsync(request, cts.Token));
    }
}