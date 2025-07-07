using System.Text.Json.Serialization;

namespace Cerebras.Cloud.Sdk.Completions;

/// <summary>
/// Represents a text completion choice.
/// </summary>
public record TextCompletionChoice
{
    /// <summary>
    /// The generated text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The index of this choice.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Log probabilities for the output tokens.
    /// </summary>
    public LogprobResult? Logprobs { get; init; }

    /// <summary>
    /// The reason the generation stopped.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
}