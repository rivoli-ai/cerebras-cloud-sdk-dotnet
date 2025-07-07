using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cerebras.Cloud.Sdk.Chat;

/// <summary>
/// Represents a chat completion request.
/// </summary>
public record ChatCompletionRequest
{
    /// <summary>
    /// The model to use for completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The messages to generate a completion for.
    /// </summary>
    public required IList<ChatMessage> Messages { get; init; }

    /// <summary>
    /// The maximum number of tokens to generate.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; init; }

    /// <summary>
    /// The temperature for sampling (0-1.5).
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// The top-p value for nucleus sampling.
    /// </summary>
    [JsonPropertyName("top_p")]
    public double? TopP { get; init; }

    /// <summary>
    /// Number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency.
    /// </summary>
    [JsonPropertyName("frequency_penalty")]
    public double? FrequencyPenalty { get; init; }

    /// <summary>
    /// Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far.
    /// </summary>
    [JsonPropertyName("presence_penalty")]
    public double? PresencePenalty { get; init; }

    /// <summary>
    /// The seed for reproducible generation.
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>
    /// Whether to stream the response.
    /// </summary>
    public bool Stream { get; init; }

    /// <summary>
    /// How many chat completion choices to generate for each input message.
    /// </summary>
    public int? N { get; init; }

    /// <summary>
    /// Up to 4 sequences where the API will stop generating further tokens.
    /// </summary>
    public IList<string>? Stop { get; init; }

    /// <summary>
    /// An object specifying the format that the model must output.
    /// </summary>
    [JsonPropertyName("response_format")]
    public ResponseFormat? ResponseFormat { get; init; }

    /// <summary>
    /// A unique identifier representing your end-user.
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// Controls which (if any) function is called by the model.
    /// </summary>
    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; init; }

    /// <summary>
    /// A list of tools the model may call.
    /// </summary>
    public IList<Tool>? Tools { get; init; }
}

/// <summary>
/// Represents a chat message.
/// </summary>
public record ChatMessage
{
    /// <summary>
    /// The role of the message author (system, user, assistant, tool).
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// The content of the message.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The name of the author of this message.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Tool calls made by the assistant.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public IList<ToolCall>? ToolCalls { get; init; }

    /// <summary>
    /// Tool call ID that this message is responding to.
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; init; }
}

/// <summary>
/// Represents a tool that can be called by the model.
/// </summary>
public record Tool
{
    /// <summary>
    /// The type of the tool (currently only "function" is supported).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The function definition.
    /// </summary>
    public required FunctionDefinition Function { get; init; }
}

/// <summary>
/// Represents a function definition.
/// </summary>
public record FunctionDefinition
{
    /// <summary>
    /// The name of the function to be called.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// A description of what the function does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The parameters the function accepts, described as a JSON Schema object.
    /// </summary>
    public object? Parameters { get; init; }
}

/// <summary>
/// Represents a tool call made by the assistant.
/// </summary>
public record ToolCall
{
    /// <summary>
    /// The ID of the tool call.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The type of the tool (currently only "function" is supported).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The function call details.
    /// </summary>
    public required FunctionCall Function { get; init; }
}

/// <summary>
/// Represents a function call.
/// </summary>
public record FunctionCall
{
    /// <summary>
    /// The name of the function to call.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The arguments to call the function with.
    /// </summary>
    public required string Arguments { get; init; }
}

/// <summary>
/// Represents the response format specification.
/// </summary>
public record ResponseFormat
{
    /// <summary>
    /// The type of response format (text or json_object).
    /// </summary>
    public required string Type { get; init; }
}

/// <summary>
/// Represents a chat completion response.
/// </summary>
public record ChatCompletionResponse
{
    /// <summary>
    /// The unique ID of the completion.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The object type (always "chat.completion").
    /// </summary>
    public required string Object { get; init; }

    /// <summary>
    /// The Unix timestamp when the completion was created.
    /// </summary>
    public required long Created { get; init; }

    /// <summary>
    /// The model used for the completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The list of completion choices.
    /// </summary>
    public required IList<ChatChoice> Choices { get; init; }

    /// <summary>
    /// Usage statistics for the completion.
    /// </summary>
    public Usage? Usage { get; init; }

    /// <summary>
    /// The system fingerprint of the model.
    /// </summary>
    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; init; }
}

/// <summary>
/// Represents a chat completion choice.
/// </summary>
public record ChatChoice
{
    /// <summary>
    /// The index of this choice.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// The message generated by the model.
    /// </summary>
    public required ChatMessage Message { get; init; }

    /// <summary>
    /// The reason the generation stopped.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }

    /// <summary>
    /// Log probabilities for the output tokens.
    /// </summary>
    public object? Logprobs { get; init; }
}

/// <summary>
/// Represents a streaming chat completion chunk.
/// </summary>
public record ChatCompletionChunk
{
    /// <summary>
    /// The unique ID of the completion.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The object type (always "chat.completion.chunk").
    /// </summary>
    public required string Object { get; init; }

    /// <summary>
    /// The Unix timestamp when the chunk was created.
    /// </summary>
    public required long Created { get; init; }

    /// <summary>
    /// The model used for the completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The list of completion choices.
    /// </summary>
    public required IList<ChatStreamChoice> Choices { get; init; }

    /// <summary>
    /// The system fingerprint of the model.
    /// </summary>
    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; init; }
}

/// <summary>
/// Represents a streaming chat completion choice.
/// </summary>
public record ChatStreamChoice
{
    /// <summary>
    /// The index of this choice.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// The delta (incremental update) for this choice.
    /// </summary>
    public required ChatMessageDelta Delta { get; init; }

    /// <summary>
    /// The reason the generation stopped.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }

    /// <summary>
    /// Log probabilities for the output tokens.
    /// </summary>
    public object? Logprobs { get; init; }
}

/// <summary>
/// Represents an incremental update to a chat message.
/// </summary>
public record ChatMessageDelta
{
    /// <summary>
    /// The role of the message author.
    /// </summary>
    public string? Role { get; init; }

    /// <summary>
    /// The incremental content.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Tool calls made by the assistant.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public IList<ToolCall>? ToolCalls { get; init; }
}