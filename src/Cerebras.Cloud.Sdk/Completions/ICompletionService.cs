using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cerebras.Cloud.Sdk.Completions;

/// <summary>
/// Service for text completions.
/// </summary>
public interface ICompletionService
{
    /// <summary>
    /// Creates a text completion.
    /// </summary>
    Task<TextCompletionResponse> CreateAsync(
        TextCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a streaming text completion.
    /// </summary>
    IAsyncEnumerable<TextCompletionChunk> CreateStreamAsync(
        TextCompletionRequest request,
        CancellationToken cancellationToken = default);
}