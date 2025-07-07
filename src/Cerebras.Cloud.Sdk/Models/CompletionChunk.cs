namespace Cerebras.Cloud.Sdk;

/// <summary>
/// Represents a chunk of a streaming completion.
/// </summary>
public record CompletionChunk
{
    /// <summary>
    /// The chunk of generated text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Whether this is the final chunk.
    /// </summary>
    public bool IsFinished { get; init; }

    /// <summary>
    /// The finish reason if this is the final chunk.
    /// </summary>
    public string? FinishReason { get; init; }
}