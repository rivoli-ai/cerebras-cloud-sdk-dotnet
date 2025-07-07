using System.Collections.Generic;
using System.Text.Json;
using Cerebras.Cloud.Sdk.Chat;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit.Chat;

public class ToolModelsTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [Fact]
    public void Tool_Serialization_Works()
    {
        // Arrange
        var tool = new Tool
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = "calculate_sum",
                Description = "Calculates the sum of two numbers",
                Parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["a"] = new Dictionary<string, string> { ["type"] = "number" },
                        ["b"] = new Dictionary<string, string> { ["type"] = "number" }
                    },
                    ["required"] = new[] { "a", "b" }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(tool, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<Tool>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("function", deserialized.Type);
        Assert.NotNull(deserialized.Function);
        Assert.Equal("calculate_sum", deserialized.Function.Name);
        Assert.Equal("Calculates the sum of two numbers", deserialized.Function.Description);
        Assert.NotNull(deserialized.Function.Parameters);
    }

    [Fact]
    public void FunctionDefinition_WithoutDescription_Works()
    {
        // Arrange
        var function = new FunctionDefinition
        {
            Name = "get_time",
            Description = null,
            Parameters = null
        };

        // Act
        var json = JsonSerializer.Serialize(function, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<FunctionDefinition>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("get_time", deserialized.Name);
        Assert.Null(deserialized.Description);
        Assert.Null(deserialized.Parameters);
    }

    [Fact]
    public void ToolCall_Serialization_Works()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            Id = "call_abc123",
            Type = "function",
            Function = new FunctionCall
            {
                Name = "search_web",
                Arguments = "{\"query\":\"Cerebras AI\"}"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(toolCall, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ToolCall>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("call_abc123", deserialized.Id);
        Assert.Equal("function", deserialized.Type);
        Assert.NotNull(deserialized.Function);
        Assert.Equal("search_web", deserialized.Function.Name);
        Assert.Equal("{\"query\":\"Cerebras AI\"}", deserialized.Function.Arguments);
    }

    [Fact]
    public void FunctionCall_EmptyArguments_Works()
    {
        // Arrange
        var functionCall = new FunctionCall
        {
            Name = "get_random_number",
            Arguments = "{}"
        };

        // Act
        var json = JsonSerializer.Serialize(functionCall, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<FunctionCall>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("get_random_number", deserialized.Name);
        Assert.Equal("{}", deserialized.Arguments);
    }

    [Fact]
    public void ResponseFormat_Serialization_Works()
    {
        // Arrange
        var format = new ResponseFormat
        {
            Type = "json_object"
        };

        // Act
        var json = JsonSerializer.Serialize(format, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ResponseFormat>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("json_object", deserialized.Type);
        Assert.Contains("\"type\":\"json_object\"", json);
    }

    [Fact]
    public void Tool_DeserializeFromJson_Works()
    {
        // Arrange
        var json = @"{
            ""type"": ""function"",
            ""function"": {
                ""name"": ""send_email"",
                ""description"": ""Send an email to a recipient"",
                ""parameters"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""to"": { ""type"": ""string"" },
                        ""subject"": { ""type"": ""string"" }
                    }
                }
            }
        }";

        // Act
        var tool = JsonSerializer.Deserialize<Tool>(json, _jsonOptions);

        // Assert
        Assert.NotNull(tool);
        Assert.Equal("function", tool.Type);
        Assert.Equal("send_email", tool.Function.Name);
        Assert.Equal("Send an email to a recipient", tool.Function.Description);
        Assert.NotNull(tool.Function.Parameters);
    }

    [Fact]
    public void FunctionDefinition_ComplexParameters_Serialize()
    {
        // Arrange
        var function = new FunctionDefinition
        {
            Name = "complex_function",
            Description = "A function with complex parameters",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    items = new
                    {
                        type = "array",
                        items = new { type = "string" }
                    },
                    options = new
                    {
                        type = "object",
                        properties = new
                        {
                            enabled = new { type = "boolean" },
                            count = new { type = "integer", minimum = 0 }
                        }
                    }
                },
                required = new[] { "items" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(function, _jsonOptions);

        // Assert
        Assert.Contains("\"name\":\"complex_function\"", json);
        Assert.Contains("\"description\":\"A function with complex parameters\"", json);
        Assert.Contains("\"type\":\"array\"", json);
        Assert.Contains("\"minimum\":0", json);
    }
}