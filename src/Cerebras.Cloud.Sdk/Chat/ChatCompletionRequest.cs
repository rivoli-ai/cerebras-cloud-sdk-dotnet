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