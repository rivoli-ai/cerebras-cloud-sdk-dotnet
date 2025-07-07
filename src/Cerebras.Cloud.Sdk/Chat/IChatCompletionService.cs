using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cerebras.Cloud.Sdk.Chat;

/// <summary>
/// Service for chat completions.
/// </summary>
public interface IChatCompletionService
{
    /// <summary>
    /// Creates a chat completion.
    /// </summary>
    Task<ChatCompletionResponse> CreateAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a streaming chat completion.
    /// </summary>
    IAsyncEnumerable<ChatCompletionChunk> CreateStreamAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}