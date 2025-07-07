using System.Text.Json.Serialization;

namespace Cerebras.Cloud.Sdk.Chat;

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