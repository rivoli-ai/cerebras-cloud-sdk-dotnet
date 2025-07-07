using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cerebras.Cloud.Sdk.Completions;

/// <summary>
/// Represents a text completion request.
/// </summary>
public record TextCompletionRequest
{
    /// <summary>
    /// The model to use for completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The prompt(s) to generate completions for.
    /// </summary>
    public required object Prompt { get; init; } // Can be string or array of strings

    /// <summary>
    /// The maximum number of tokens to generate.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; init; }

    /// <summary>
    /// The temperature for sampling (0-2).
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// The top-p value for nucleus sampling.
    /// </summary>
    [JsonPropertyName("top_p")]
    public double? TopP { get; init; }

    /// <summary>
    /// How many completions to generate for each prompt.
    /// </summary>
    public int? N { get; init; }

    /// <summary>
    /// Whether to stream the response.
    /// </summary>
    public bool Stream { get; init; }

    /// <summary>
    /// Include the log probabilities on the logprobs most likely tokens.
    /// </summary>
    public int? Logprobs { get; init; }

    /// <summary>
    /// Echo back the prompt in addition to the completion.
    /// </summary>
    public bool? Echo { get; init; }

    /// <summary>
    /// Up to 4 sequences where the API will stop generating further tokens.
    /// </summary>
    public object? Stop { get; init; } // Can be string or array of strings

    /// <summary>
    /// Number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency.
    /// </summary>
    [JsonPropertyName("frequency_penalty")]
    public double? FrequencyPenalty { get; init; }

    /// <summary>
    /// Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far.
    /// </summary>
    [JsonPropertyName("presence_penalty")]
    public double? PresencePenalty { get; init; }

    /// <summary>
    /// Generates best_of completions server-side and returns the "best".
    /// </summary>
    [JsonPropertyName("best_of")]
    public int? BestOf { get; init; }

    /// <summary>
    /// Modify the likelihood of specified tokens appearing in the completion.
    /// </summary>
    [JsonPropertyName("logit_bias")]
    public Dictionary<string, double>? LogitBias { get; init; }

    /// <summary>
    /// A unique identifier representing your end-user.
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// The suffix that comes after a completion of inserted text.
    /// </summary>
    public string? Suffix { get; init; }

    /// <summary>
    /// The seed for reproducible generation.
    /// </summary>
    public int? Seed { get; init; }
}

/// <summary>
/// Represents a text completion response.
/// </summary>
public record TextCompletionResponse
{
    /// <summary>
    /// The unique ID of the completion.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The object type (always "text_completion").
    /// </summary>
    public required string Object { get; init; }

    /// <summary>
    /// The Unix timestamp when the completion was created.
    /// </summary>
    public required long Created { get; init; }

    /// <summary>
    /// The model used for the completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The list of completion choices.
    /// </summary>
    public required IList<TextCompletionChoice> Choices { get; init; }

    /// <summary>
    /// Usage statistics for the completion.
    /// </summary>
    public Usage? Usage { get; init; }
}

/// <summary>
/// Represents a text completion choice.
/// </summary>
public record TextCompletionChoice
{
    /// <summary>
    /// The generated text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The index of this choice.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Log probabilities for the output tokens.
    /// </summary>
    public LogprobResult? Logprobs { get; init; }

    /// <summary>
    /// The reason the generation stopped.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
}

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

/// <summary>
/// Represents a streaming text completion chunk.
/// </summary>
public record TextCompletionChunk
{
    /// <summary>
    /// The unique ID of the completion.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The object type (always "text_completion.chunk").
    /// </summary>
    public required string Object { get; init; }

    /// <summary>
    /// The Unix timestamp when the chunk was created.
    /// </summary>
    public required long Created { get; init; }

    /// <summary>
    /// The model used for the completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The list of completion choices.
    /// </summary>
    public required IList<TextCompletionStreamChoice> Choices { get; init; }
}

/// <summary>
/// Represents a streaming text completion choice.
/// </summary>
public record TextCompletionStreamChoice
{
    /// <summary>
    /// The incremental text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The index of this choice.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Log probabilities for the output tokens.
    /// </summary>
    public LogprobResult? Logprobs { get; init; }

    /// <summary>
    /// The reason the generation stopped.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
}