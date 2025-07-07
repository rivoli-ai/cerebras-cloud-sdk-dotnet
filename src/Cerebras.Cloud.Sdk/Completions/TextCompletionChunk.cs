using System.Collections.Generic;

namespace Cerebras.Cloud.Sdk.Completions;

/// <summary>
/// Represents a streaming text completion chunk.
/// </summary>
public record TextCompletionChunk
{
    /// <summary>
    /// The unique ID of the completion.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The object type (always "text_completion.chunk").
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
    public required IList<TextCompletionStreamChoice> Choices { get; init; }
}