using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cerebras.Cloud.Sdk.Chat;

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