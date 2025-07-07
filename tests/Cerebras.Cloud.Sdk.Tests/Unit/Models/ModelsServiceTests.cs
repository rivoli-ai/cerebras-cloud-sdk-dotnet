using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Http;
using Cerebras.Cloud.Sdk.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit.Models;

public class ModelsServiceTests
{
    private readonly Mock<IHttpService> _httpServiceMock;
    private readonly Mock<ILogger<ModelsService>> _loggerMock;
    private readonly ModelsService _service;
    private readonly JsonSerializerOptions _jsonOptions;

    public ModelsServiceTests()
    {
        _httpServiceMock = new Mock<IHttpService>();
        _loggerMock = new Mock<ILogger<ModelsService>>();
        _service = new ModelsService(_httpServiceMock.Object, _loggerMock.Object);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelsService(null!, _loggerMock.Object));
        Assert.Throws<ArgumentNullException>(() => new ModelsService(_httpServiceMock.Object, null!));
    }

    [Fact]
    public async Task ListAsync_Success_ReturnsModelList()
    {
        // Arrange
        var responseData = new
        {
            @object = "list",
            data = new object[]
            {
                new
                {
                    id = "llama-3.1-8b",
                    @object = "model",
                    created = 1234567890,
                    owned_by = "cerebras",
                    name = "Llama 3.1 8B",
                    description = "Fast 8B model",
                    context_length = 8192,
                    is_available = true
                },
                new
                {
                    id = "llama-3.1-70b",
                    @object = "model",
                    created = 1234567890,
                    owned_by = "cerebras",
                    name = (string?)null,
                    description = "Powerful 70B model",
                    context_length = 8192,
                    is_available = (bool?)null
                }
            }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseData, _jsonOptions))
        };

        _httpServiceMock
            .Setup(x => x.SendAsync(It.Is<HttpRequestMessage>(r => 
                r.Method == HttpMethod.Get && 
                r.RequestUri!.ToString() == "models"), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.ListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        
        Assert.Equal("llama-3.1-8b", result[0].Id);
        Assert.Equal("Llama 3.1 8B", result[0].Name);
        Assert.Equal("Fast 8B model", result[0].Description);
        Assert.Equal(8192, result[0].ContextWindow);
        Assert.True(result[0].IsAvailable);
        
        Assert.Equal("llama-3.1-70b", result[1].Id);
        Assert.Equal("llama-3.1-70b", result[1].Name); // Should default to ID when name is null
        Assert.Equal("Powerful 70B model", result[1].Description);
        Assert.True(result[1].IsAvailable); // Should default to true when null
    }

    [Fact]
    public async Task ListAsync_InvalidResponse_ThrowsException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        _httpServiceMock
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(httpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.ListAsync());
        
        Assert.Equal("Failed to parse models response", exception.Message);
    }

    [Fact]
    public async Task RetrieveAsync_Success_ReturnsModel()
    {
        // Arrange
        var modelId = "llama-3.1-70b";
        var responseData = new
        {
            id = modelId,
            @object = "model",
            created = 1234567890,
            owned_by = "cerebras",
            name = "Llama 3.1 70B",
            description = "Powerful 70B model",
            context_length = 8192,
            is_available = true
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseData, _jsonOptions))
        };

        _httpServiceMock
            .Setup(x => x.SendAsync(It.Is<HttpRequestMessage>(r => 
                r.Method == HttpMethod.Get && 
                r.RequestUri!.ToString() == $"models/{modelId}"), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.RetrieveAsync(modelId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(modelId, result.Id);
        Assert.Equal("Llama 3.1 70B", result.Name);
        Assert.Equal("Powerful 70B model", result.Description);
        Assert.Equal(8192, result.ContextWindow);
        Assert.True(result.IsAvailable);
    }

    [Fact]
    public async Task RetrieveAsync_EmptyModelId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.RetrieveAsync(""));
        
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.RetrieveAsync(null!));
    }

    [Fact]
    public async Task RetrieveAsync_InvalidResponse_ThrowsException()
    {
        // Arrange
        var modelId = "test-model";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        _httpServiceMock
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(httpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.RetrieveAsync(modelId));
        
        Assert.Equal($"Failed to parse model response for '{modelId}'", exception.Message);
    }

    [Fact]
    public async Task ListAsync_LogsCorrectly()
    {
        // Arrange
        var responseData = new
        {
            @object = "list",
            data = new[] { new { id = "model1", @object = "model" } }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseData, _jsonOptions))
        };

        _httpServiceMock
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(httpResponse);

        // Act
        await _service.ListAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Listing available models")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved 1 models")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}