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

public class ToolCallingTests
{
    private readonly Mock<IHttpService> _httpServiceMock;
    private readonly Mock<ILogger<ChatCompletionService>> _loggerMock;
    private readonly ChatCompletionService _service;
    private readonly JsonSerializerOptions _jsonOptions;

    public ToolCallingTests()
    {
        _httpServiceMock = new Mock<IHttpService>();
        _loggerMock = new Mock<ILogger<ChatCompletionService>>();
        _service = new ChatCompletionService(_httpServiceMock.Object, _loggerMock.Object);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    [Fact]
    public async Task CreateAsync_WithTools_SendsCorrectRequest()
    {
        // Arrange
        var tools = new List<Tool>
        {
            new()
            {
                Type = "function",
                Function = new FunctionDefinition
                {
                    Name = "get_weather",
                    Description = "Get the current weather for a location",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            location = new { type = "string", description = "The city and state" },
                            unit = new { type = "string", @enum = new[] { "celsius", "fahrenheit" } }
                        },
                        required = new[] { "location" }
                    }
                }
            },
            new()
            {
                Type = "function", 
                Function = new FunctionDefinition
                {
                    Name = "search_web",
                    Description = "Search the web for information",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            query = new { type = "string", description = "Search query" }
                        },
                        required = new[] { "query" }
                    }
                }
            }
        };

        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "What's the weather in Paris?" }
            },
            Tools = tools,
            ToolChoice = "auto"
        };

        var responseData = new
        {
            id = "chatcmpl-123",
            @object = "chat.completion",
            created = 1234567890,
            model = "llama-3.3-70b",
            choices = new[]
            {
                new
                {
                    index = 0,
                    message = new
                    {
                        role = "assistant",
                        content = (string?)null,
                        tool_calls = new[]
                        {
                            new
                            {
                                id = "call_123",
                                type = "function",
                                function = new
                                {
                                    name = "get_weather",
                                    arguments = "{\"location\":\"Paris, France\"}"
                                }
                            }
                        }
                    },
                    finish_reason = "tool_calls"
                }
            }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseData, _jsonOptions))
        };

        HttpRequestMessage? capturedRequest = null;
        _httpServiceMock
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Single(response.Choices);
        Assert.Null(response.Choices[0].Message.Content);
        Assert.NotNull(response.Choices[0].Message.ToolCalls);
        Assert.Single(response.Choices[0].Message.ToolCalls!);
        
        var toolCall = response.Choices[0].Message.ToolCalls![0];
        Assert.Equal("call_123", toolCall.Id);
        Assert.Equal("function", toolCall.Type);
        Assert.Equal("get_weather", toolCall.Function.Name);
        Assert.Equal("{\"location\":\"Paris, France\"}", toolCall.Function.Arguments);
        
        // Verify request was sent correctly
        Assert.NotNull(capturedRequest);
        var requestContent = await capturedRequest!.Content!.ReadAsStringAsync();
        var sentRequest = JsonSerializer.Deserialize<JsonElement>(requestContent);
        
        Assert.True(sentRequest.TryGetProperty("tools", out var sentTools));
        Assert.Equal(2, sentTools.GetArrayLength());
        Assert.Equal("get_weather", sentTools[0].GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("search_web", sentTools[1].GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("auto", sentRequest.GetProperty("tool_choice").GetString());
    }

    [Fact]
    public async Task CreateAsync_WithToolResponse_HandlesCorrectly()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "What's the weather in Paris?" },
                new() 
                { 
                    Role = "assistant", 
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
                                Arguments = "{\"location\":\"Paris, France\"}"
                            }
                        }
                    }
                },
                new() 
                { 
                    Role = "tool", 
                    Content = "{\"temperature\": \"22°C\", \"condition\": \"Sunny\"}",
                    ToolCallId = "call_123"
                }
            }
        };

        var responseData = new
        {
            id = "chatcmpl-456",
            @object = "chat.completion",
            created = 1234567890,
            model = "llama-3.3-70b",
            choices = new[]
            {
                new
                {
                    index = 0,
                    message = new
                    {
                        role = "assistant",
                        content = "The weather in Paris is currently sunny with a temperature of 22°C."
                    },
                    finish_reason = "stop"
                }
            }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseData, _jsonOptions))
        };

        HttpRequestMessage? capturedRequest = null;
        _httpServiceMock
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("sunny", response.Choices[0].Message.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("22°C", response.Choices[0].Message.Content);
        
        // Verify the tool response was included in the request
        Assert.NotNull(capturedRequest);
        var requestContent = await capturedRequest!.Content!.ReadAsStringAsync();
        var sentRequest = JsonSerializer.Deserialize<JsonElement>(requestContent);
        var messages = sentRequest.GetProperty("messages");
        
        Assert.Equal(3, messages.GetArrayLength());
        
        // Check tool message
        var toolMessage = messages[2];
        Assert.Equal("tool", toolMessage.GetProperty("role").GetString());
        Assert.Equal("call_123", toolMessage.GetProperty("tool_call_id").GetString());
        Assert.Contains("22°C", toolMessage.GetProperty("content").GetString());
    }

    [Fact]
    public async Task CreateStreamAsync_WithToolCalls_HandlesStreamingToolCalls()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Get weather for Paris" }
            },
            Tools = new List<Tool>
            {
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition
                    {
                        Name = "get_weather",
                        Description = "Get weather for a location"
                    }
                }
            },
            Stream = true
        };

        // Create proper ChatCompletionChunk objects
        var chunks = new List<ChatCompletionChunk>
        {
            new()
            {
                Id = "chatcmpl-1",
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
                            ToolCalls = new List<ToolCall>
                            {
                                new()
                                {
                                    Id = "call_1",
                                    Type = "function",
                                    Function = new FunctionCall
                                    {
                                        Name = "get_weather",
                                        Arguments = ""
                                    }
                                }
                            }
                        },
                        FinishReason = null
                    }
                }
            },
            new()
            {
                Id = "chatcmpl-2",
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
                            ToolCalls = new List<ToolCall>
                            {
                                new()
                                {
                                    Id = "",
                                    Type = "function",
                                    Function = new FunctionCall
                                    {
                                        Name = "",
                                        Arguments = "{\"location\":"
                                    }
                                }
                            }
                        },
                        FinishReason = null
                    }
                }
            },
            new()
            {
                Id = "chatcmpl-3",
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
                            ToolCalls = new List<ToolCall>
                            {
                                new()
                                {
                                    Id = "",
                                    Type = "function",
                                    Function = new FunctionCall
                                    {
                                        Name = "",
                                        Arguments = "\"Paris\"}"
                                    }
                                }
                            }
                        },
                        FinishReason = null
                    }
                }
            },
            new()
            {
                Id = "chatcmpl-4",
                Object = "chat.completion.chunk",
                Created = 1234567890,
                Model = "llama-3.3-70b",
                Choices = new List<ChatStreamChoice>
                {
                    new()
                    {
                        Index = 0,
                        Delta = new ChatMessageDelta(),
                        FinishReason = "tool_calls"
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
        var receivedChunks = new List<ChatCompletionChunk>();
        await foreach (var chunk in _service.CreateStreamAsync(request))
        {
            receivedChunks.Add(chunk);
        }

        // Assert
        Assert.Equal(4, receivedChunks.Count);
        
        // First chunk should have the tool call start
        var firstChunk = receivedChunks[0];
        Assert.NotNull(firstChunk.Choices[0].Delta.ToolCalls);
        Assert.Single(firstChunk.Choices[0].Delta.ToolCalls!);
        Assert.Equal("call_1", firstChunk.Choices[0].Delta.ToolCalls![0].Id);
        Assert.Equal("get_weather", firstChunk.Choices[0].Delta.ToolCalls![0].Function?.Name);
        
        // Middle chunks should have arguments
        Assert.Contains("location", receivedChunks[1].Choices[0].Delta.ToolCalls?[0].Function?.Arguments ?? "");
        Assert.Contains("Paris", receivedChunks[2].Choices[0].Delta.ToolCalls?[0].Function?.Arguments ?? "");
        
        // Last chunk should have finish reason
        Assert.Equal("tool_calls", receivedChunks[3].Choices[0].FinishReason);
    }

    [Fact]
    public void ChatMessage_WithToolCalls_SerializesCorrectly()
    {
        // Arrange
        var message = new ChatMessage
        {
            Role = "assistant",
            Content = null,
            ToolCalls = new List<ToolCall>
            {
                new()
                {
                    Id = "call_abc123",
                    Type = "function",
                    Function = new FunctionCall
                    {
                        Name = "calculate",
                        Arguments = "{\"x\": 5, \"y\": 10}"
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<JsonElement>(json);

        // Assert
        Assert.Equal("assistant", deserialized.GetProperty("role").GetString());
        Assert.True(deserialized.TryGetProperty("content", out var content));
        Assert.Equal(JsonValueKind.Null, content.ValueKind);
        
        var toolCalls = deserialized.GetProperty("tool_calls");
        Assert.Equal(1, toolCalls.GetArrayLength());
        
        var toolCall = toolCalls[0];
        Assert.Equal("call_abc123", toolCall.GetProperty("id").GetString());
        Assert.Equal("function", toolCall.GetProperty("type").GetString());
        Assert.Equal("calculate", toolCall.GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("{\"x\": 5, \"y\": 10}", toolCall.GetProperty("function").GetProperty("arguments").GetString());
    }

    [Fact]
    public void Tool_SerializesCorrectly()
    {
        // Arrange
        var tool = new Tool
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = "get_stock_price",
                Description = "Get the current stock price for a ticker symbol",
                Parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["ticker"] = new Dictionary<string, string>
                        {
                            ["type"] = "string",
                            ["description"] = "Stock ticker symbol (e.g., AAPL)"
                        }
                    },
                    ["required"] = new[] { "ticker" }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(tool, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<JsonElement>(json);

        // Assert
        Assert.Equal("function", deserialized.GetProperty("type").GetString());
        
        var function = deserialized.GetProperty("function");
        Assert.Equal("get_stock_price", function.GetProperty("name").GetString());
        Assert.Equal("Get the current stock price for a ticker symbol", function.GetProperty("description").GetString());
        
        var parameters = function.GetProperty("parameters");
        Assert.Equal("object", parameters.GetProperty("type").GetString());
        Assert.True(parameters.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("ticker", out var ticker));
        Assert.Equal("string", ticker.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("none")]
    [InlineData("get_weather")]
    [InlineData(null)]
    public async Task CreateAsync_WithDifferentToolChoices_SendsCorrectValue(string? toolChoice)
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "llama-3.3-70b",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Hello" }
            },
            Tools = new List<Tool>
            {
                new()
                {
                    Type = "function",
                    Function = new FunctionDefinition { Name = "test_function" }
                }
            },
            ToolChoice = toolChoice
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                id = "test",
                @object = "chat.completion",
                created = 1234567890,
                model = "llama-3.3-70b",
                choices = new[] { new { index = 0, message = new { role = "assistant", content = "Hi" }, finish_reason = "stop" } }
            }, _jsonOptions))
        };

        HttpRequestMessage? capturedRequest = null;
        _httpServiceMock
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(httpResponse);

        // Act
        await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        var requestContent = await capturedRequest!.Content!.ReadAsStringAsync();
        var sentRequest = JsonSerializer.Deserialize<JsonElement>(requestContent);
        
        if (toolChoice != null)
        {
            Assert.True(sentRequest.TryGetProperty("tool_choice", out var sentToolChoice));
            Assert.Equal(toolChoice, sentToolChoice.GetString());
        }
        else
        {
            Assert.False(sentRequest.TryGetProperty("tool_choice", out _));
        }
    }
}