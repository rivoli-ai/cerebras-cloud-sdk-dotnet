namespace Cerebras.Cloud.Sdk;

/// <summary>
/// Represents token usage statistics.
/// </summary>
public record Usage
{
    /// <summary>
    /// The number of prompt tokens.
    /// </summary>
    public int PromptTokens { get; init; }

    /// <summary>
    /// The number of completion tokens.
    /// </summary>
    public int CompletionTokens { get; init; }

    /// <summary>
    /// The total number of tokens.
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
}