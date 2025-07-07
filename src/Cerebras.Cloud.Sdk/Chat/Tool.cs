namespace Cerebras.Cloud.Sdk.Chat;

/// <summary>
/// Represents a tool that can be called by the model.
/// </summary>
public record Tool
{
    /// <summary>
    /// The type of the tool (currently only "function" is supported).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The function definition.
    /// </summary>
    public required FunctionDefinition Function { get; init; }
}