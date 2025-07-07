using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cerebras.Cloud.Sdk;

/// <summary>
/// Defines the contract for interacting with the Cerebras AI API.
/// </summary>
public interface ICerebrasClient
{
    /// <summary>
    /// Generates a completion for the given prompt.
    /// </summary>
    /// <param name="request">The completion request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completion response.</returns>
    Task<CompletionResponse> GenerateCompletionAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming completion for the given prompt.
    /// </summary>
    /// <param name="request">The completion request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of completion chunks.</returns>
    IAsyncEnumerable<CompletionChunk> GenerateCompletionStreamAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available models.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of available models.</returns>
    Task<IReadOnlyList<Model>> ListModelsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The model information.</returns>
    Task<Model> GetModelAsync(
        string modelId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a completion request.
/// </summary>
public record CompletionRequest
{
    /// <summary>
    /// The model to use for completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The prompt to generate a completion for.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// The maximum number of tokens to generate.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// The temperature for sampling.
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// The top-p value for nucleus sampling.
    /// </summary>
    public double? TopP { get; init; }

    /// <summary>
    /// The seed for reproducible generation.
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>
    /// Whether to stream the response.
    /// </summary>
    public bool Stream { get; init; }
}

/// <summary>
/// Represents a completion response.
/// </summary>
public record CompletionResponse
{
    /// <summary>
    /// The unique ID of the completion.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The model used for the completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The generated text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The reason the generation stopped.
    /// </summary>
    public string? FinishReason { get; init; }

    /// <summary>
    /// Usage statistics for the completion.
    /// </summary>
    public Usage? Usage { get; init; }

    /// <summary>
    /// The timestamp when the completion was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Represents a chunk of a streaming completion.
/// </summary>
public record CompletionChunk
{
    /// <summary>
    /// The chunk of generated text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Whether this is the final chunk.
    /// </summary>
    public bool IsFinished { get; init; }

    /// <summary>
    /// The finish reason if this is the final chunk.
    /// </summary>
    public string? FinishReason { get; init; }
}

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