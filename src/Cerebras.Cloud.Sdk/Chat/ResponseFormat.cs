namespace Cerebras.Cloud.Sdk.Chat;

/// <summary>
/// Represents the response format specification.
/// </summary>
public record ResponseFormat
{
    /// <summary>
    /// The type of response format (text or json_object).
    /// </summary>
    public required string Type { get; init; }
}