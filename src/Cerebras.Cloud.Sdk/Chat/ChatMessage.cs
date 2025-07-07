using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cerebras.Cloud.Sdk.Chat;

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
    public string? Content { get; init; }

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