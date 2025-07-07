namespace Cerebras.Cloud.Sdk;

/// <summary>
/// Represents a Cerebras model.
/// </summary>
public record Model
{
    /// <summary>
    /// The unique ID of the model.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The name of the model.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The description of the model.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The context window size.
    /// </summary>
    public int? ContextWindow { get; init; }

    /// <summary>
    /// Whether the model is available.
    /// </summary>
    public bool IsAvailable { get; init; }
}