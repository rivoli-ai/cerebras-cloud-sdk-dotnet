using System.Collections.Generic;

namespace Cerebras.Cloud.Sdk.Completions;

/// <summary>
/// Represents a text completion response.
/// </summary>
public record TextCompletionResponse
{
    /// <summary>
    /// The unique ID of the completion.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The object type (always "text_completion").
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
    public required IList<TextCompletionChoice> Choices { get; init; }

    /// <summary>
    /// Usage statistics for the completion.
    /// </summary>
    public Usage? Usage { get; init; }
}