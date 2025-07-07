using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cerebras.Cloud.Sdk.Completions;

/// <summary>
/// Represents log probability information.
/// </summary>
public record LogprobResult
{
    /// <summary>
    /// The tokens generated.
    /// </summary>
    public IList<string>? Tokens { get; init; }

    /// <summary>
    /// The log probabilities of the tokens.
    /// </summary>
    [JsonPropertyName("token_logprobs")]
    public IList<double?>? TokenLogprobs { get; init; }

    /// <summary>
    /// The top log probabilities for each token position.
    /// </summary>
    [JsonPropertyName("top_logprobs")]
    public IList<Dictionary<string, double>?>? TopLogprobs { get; init; }

    /// <summary>
    /// The text offset of each token.
    /// </summary>
    [JsonPropertyName("text_offset")]
    public IList<int>? TextOffset { get; init; }
}