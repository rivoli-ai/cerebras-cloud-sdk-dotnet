namespace Cerebras.Cloud.Sdk.Chat;

/// <summary>
/// Represents a function call.
/// </summary>
public record FunctionCall
{
    /// <summary>
    /// The name of the function to call.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The arguments to call the function with.
    /// </summary>
    public required string Arguments { get; init; }
}