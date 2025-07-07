namespace Cerebras.Cloud.Sdk.Chat;

/// <summary>
/// Represents a tool call made by the assistant.
/// </summary>
public record ToolCall
{
    /// <summary>
    /// The ID of the tool call.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The type of the tool (currently only "function" is supported).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The function call details.
    /// </summary>
    public required FunctionCall Function { get; init; }
}