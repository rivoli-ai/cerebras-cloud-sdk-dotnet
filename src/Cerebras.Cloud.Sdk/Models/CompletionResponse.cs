using System;

namespace Cerebras.Cloud.Sdk;

/// <summary>
/// Represents a completion response.
/// </summary>
public record CompletionResponse
{
    /// <summary>
    /// The unique ID of the completion.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The model used for the completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The generated text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The reason the generation stopped.
    /// </summary>
    public string? FinishReason { get; init; }

    /// <summary>
    /// Usage statistics for the completion.
    /// </summary>
    public Usage? Usage { get; init; }

    /// <summary>
    /// The timestamp when the completion was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}