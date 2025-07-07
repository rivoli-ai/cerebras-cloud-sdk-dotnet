namespace Cerebras.Cloud.Sdk.Chat;

/// <summary>
/// Represents a function definition.
/// </summary>
public record FunctionDefinition
{
    /// <summary>
    /// The name of the function to be called.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// A description of what the function does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The parameters the function accepts, described as a JSON Schema object.
    /// </summary>
    public object? Parameters { get; init; }
}