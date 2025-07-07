using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Chat;
using Cerebras.Cloud.Sdk.Completions;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit;

public class CerebrasClientV2Tests
{
    private readonly Mock<IChatCompletionService> _chatServiceMock;
    private readonly Mock<ICompletionService> _completionServiceMock;
    private readonly Mock<IModelsService> _modelsServiceMock;
    private readonly Mock<ILogger<CerebrasClientV2>> _loggerMock;
    private readonly CerebrasClientV2 _client;

    public CerebrasClientV2Tests()
    {
        _chatServiceMock = new Mock<IChatCompletionService>();
        _completionServiceMock = new Mock<ICompletionService>();
        _modelsServiceMock = new Mock<IModelsService>();
        _loggerMock = new Mock<ILogger<CerebrasClientV2>>();

        var options = new CerebrasClientOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.cerebras.ai/v1/",
            DefaultModel = "llama3.1-70b"
        };

        _client = new CerebrasClientV2(
            _chatServiceMock.Object,
            _completionServiceMock.Object,
            _modelsServiceMock.Object,
            _loggerMock.Object,
            Options.Create(options));
    }

    [Fact]
    public void Constructor_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CerebrasClientOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CerebrasClientV2(
            null!,
            _completionServiceMock.Object,
            _modelsServiceMock.Object,
            _loggerMock.Object,
            options));

        Assert.Throws<ArgumentNullException>(() => new CerebrasClientV2(
            _chatServiceMock.Object,
            null!,
            _modelsServiceMock.Object,
            _loggerMock.Object,
            options));

        Assert.Throws<ArgumentNullException>(() => new CerebrasClientV2(
            _chatServiceMock.Object,
            _completionServiceMock.Object,
            null!,
            _loggerMock.Object,
            options));
    }

    [Fact]
    public void Properties_ReturnCorrectServices()
    {
        // Assert
        Assert.Same(_chatServiceMock.Object, _client.Chat);
        Assert.Same(_completionServiceMock.Object, _client.Completions);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_CallsServiceCorrectly()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "test-model",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Hello" }
            }
        };

        var expectedResponse = new ChatCompletionResponse
        {
            Id = "test-id",
            Object = "chat.completion",
            Created = 1234567890,
            Model = "test-model",
            Choices = new List<ChatChoice>()
        };

        _chatServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _client.CreateChatCompletionAsync(request);

        // Assert
        Assert.Same(expectedResponse, result);
        _chatServiceMock.Verify(x => x.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateChatCompletionStreamAsync_CallsServiceCorrectly()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "test-model",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Hello" }
            },
            Stream = true
        };

        var chunks = new List<ChatCompletionChunk>
        {
            new()
            {
                Id = "test-id",
                Object = "chat.completion.chunk",
                Created = 1234567890,
                Model = "test-model",
                Choices = new List<ChatStreamChoice>()
            }
        };

        _chatServiceMock
            .Setup(x => x.CreateStreamAsync(request, It.IsAny<CancellationToken>()))
            .Returns(chunks.ToAsyncEnumerable());

        // Act
        var result = await _client.CreateChatCompletionStreamAsync(request).ToListAsync();

        // Assert
        Assert.Single(result);
        _chatServiceMock.Verify(x => x.CreateStreamAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTextCompletionAsync_CallsServiceCorrectly()
    {
        // Arrange
        var request = new TextCompletionRequest
        {
            Model = "test-model",
            Prompt = "Hello"
        };

        var expectedResponse = new TextCompletionResponse
        {
            Id = "test-id",
            Object = "text_completion",
            Created = 1234567890,
            Model = "test-model",
            Choices = new List<TextCompletionChoice>()
        };

        _completionServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _client.CreateTextCompletionAsync(request);

        // Assert
        Assert.Same(expectedResponse, result);
        _completionServiceMock.Verify(x => x.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateCompletionAsync_ConvertsToChat_ReturnsCorrectly()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "test-model",
            Prompt = "Hello",
            MaxTokens = 100,
            Temperature = 0.7,
            TopP = 0.9,
            Seed = 42
        };

        var chatResponse = new ChatCompletionResponse
        {
            Id = "test-id",
            Object = "chat.completion",
            Created = 1234567890,
            Model = "test-model",
            Choices = new List<ChatChoice>
            {
                new()
                {
                    Index = 0,
                    Message = new ChatMessage { Role = "assistant", Content = "Hello response" },
                    FinishReason = "stop"
                }
            },
            Usage = new Usage { PromptTokens = 10, CompletionTokens = 20 }
        };

        _chatServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        // Act
        var result = await _client.GenerateCompletionAsync(request);

        // Assert
        Assert.Equal("test-id", result.Id);
        Assert.Equal("test-model", result.Model);
        Assert.Equal("Hello response", result.Text);
        Assert.Equal("stop", result.FinishReason);
        Assert.Equal(10, result.Usage?.PromptTokens);
        Assert.Equal(20, result.Usage?.CompletionTokens);
    }

    [Fact]
    public async Task GenerateCompletionStreamAsync_ConvertsToChat_StreamsCorrectly()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "test-model",
            Prompt = "Hello",
            Stream = true
        };

        var chatChunks = new List<ChatCompletionChunk>
        {
            new()
            {
                Id = "test-id",
                Object = "chat.completion.chunk",
                Created = 1234567890,
                Model = "test-model",
                Choices = new List<ChatStreamChoice>
                {
                    new()
                    {
                        Index = 0,
                        Delta = new ChatMessageDelta { Content = "Hello" }
                    }
                }
            },
            new()
            {
                Id = "test-id",
                Object = "chat.completion.chunk",
                Created = 1234567890,
                Model = "test-model",
                Choices = new List<ChatStreamChoice>
                {
                    new()
                    {
                        Index = 0,
                        Delta = new ChatMessageDelta { Content = " world" },
                        FinishReason = "stop"
                    }
                }
            }
        };

        _chatServiceMock
            .Setup(x => x.CreateStreamAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(chatChunks.ToAsyncEnumerable());

        // Act
        var chunks = await _client.GenerateCompletionStreamAsync(request).ToListAsync();

        // Assert
        Assert.Equal(2, chunks.Count);
        Assert.Equal("Hello", chunks[0].Text);
        Assert.False(chunks[0].IsFinished);
        Assert.Equal(" world", chunks[1].Text);
        Assert.True(chunks[1].IsFinished);
        Assert.Equal("stop", chunks[1].FinishReason);
    }

    [Fact]
    public async Task ListModelsAsync_CallsServiceCorrectly()
    {
        // Arrange
        var expectedModels = new List<Model>
        {
            new() { Id = "model1", Name = "Model 1", IsAvailable = true },
            new() { Id = "model2", Name = "Model 2", IsAvailable = true }
        };

        _modelsServiceMock
            .Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedModels);

        // Act
        var result = await _client.ListModelsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        _modelsServiceMock.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetModelAsync_CallsServiceCorrectly()
    {
        // Arrange
        var modelId = "test-model";
        var expectedModel = new Model
        {
            Id = modelId,
            Name = "Test Model",
            IsAvailable = true
        };

        _modelsServiceMock
            .Setup(x => x.RetrieveAsync(modelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _client.GetModelAsync(modelId);

        // Assert
        Assert.Same(expectedModel, result);
        _modelsServiceMock.Verify(x => x.RetrieveAsync(modelId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_WithoutApiKey_TriesToGetFromEnvironment()
    {
        // Arrange
        var options = new CerebrasClientOptions
        {
            ApiKey = null,
            BaseUrl = "https://api.cerebras.ai/v1/"
        };

        Environment.SetEnvironmentVariable("CEREBRAS_API_KEY", "env-test-key");

        try
        {
            // Act
            var client = new CerebrasClientV2(
                _chatServiceMock.Object,
                _completionServiceMock.Object,
                _modelsServiceMock.Object,
                _loggerMock.Object,
                Options.Create(options));

            // Assert - should not throw
            Assert.NotNull(client);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CEREBRAS_API_KEY", null);
        }
    }
}

// Extension to convert List to IAsyncEnumerable for testing
internal static class TestAsyncEnumerableExtensions
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