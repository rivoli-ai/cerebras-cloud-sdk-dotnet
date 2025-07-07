using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Chat;
using Cerebras.Cloud.Sdk.Completions;

namespace Cerebras.Cloud.Sdk;

/// <summary>
/// Enhanced interface for interacting with the Cerebras AI API.
/// </summary>
public interface ICerebrasClientV2 : ICerebrasClient
{
    /// <summary>
    /// Gets the chat completion service.
    /// </summary>
    IChatCompletionService Chat { get; }

    /// <summary>
    /// Gets the text completion service.
    /// </summary>
    ICompletionService Completions { get; }

    /// <summary>
    /// Creates a chat completion.
    /// </summary>
    Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a streaming chat completion.
    /// </summary>
    IAsyncEnumerable<ChatCompletionChunk> CreateChatCompletionStreamAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a text completion.
    /// </summary>
    Task<TextCompletionResponse> CreateTextCompletionAsync(
        TextCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a streaming text completion.
    /// </summary>
    IAsyncEnumerable<TextCompletionChunk> CreateTextCompletionStreamAsync(
        TextCompletionRequest request,
        CancellationToken cancellationToken = default);
}