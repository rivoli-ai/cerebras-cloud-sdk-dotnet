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