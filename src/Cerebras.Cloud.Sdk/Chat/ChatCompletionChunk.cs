using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cerebras.Cloud.Sdk.Chat;

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