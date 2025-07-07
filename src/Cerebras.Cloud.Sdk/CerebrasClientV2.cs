using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Chat;
using Cerebras.Cloud.Sdk.Completions;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cerebras.Cloud.Sdk;

/// <summary>
/// Enhanced client for interacting with the Cerebras AI API.
/// </summary>
public class CerebrasClientV2 : ICerebrasClientV2
{
    private readonly IChatCompletionService _chatService;
    private readonly ICompletionService _completionService;
    private readonly IModelsService _modelsService;
    private readonly ILogger<CerebrasClientV2> _logger;
    private readonly CerebrasClientOptions _options;

    /// <inheritdoc />
    public IChatCompletionService Chat => _chatService;

    /// <inheritdoc />
    public ICompletionService Completions => _completionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CerebrasClientV2"/> class.
    /// </summary>
    public CerebrasClientV2(
        IChatCompletionService chatService,
        ICompletionService completionService,
        IModelsService modelsService,
        ILogger<CerebrasClientV2> logger,
        IOptions<CerebrasClientOptions> options)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
        _modelsService = modelsService ?? throw new ArgumentNullException(nameof(modelsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // If API key is not set in options, try to get it from environment variable
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _options.ApiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");
        }
    }

    /// <inheritdoc />
    public async Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _chatService.CreateAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatCompletionChunk> CreateChatCompletionStreamAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _chatService.CreateStreamAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TextCompletionResponse> CreateTextCompletionAsync(
        TextCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _completionService.CreateAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TextCompletionChunk> CreateTextCompletionStreamAsync(
        TextCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _completionService.CreateStreamAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CompletionResponse> GenerateCompletionAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Convert to chat completion for backward compatibility
        var chatRequest = new ChatCompletionRequest
        {
            Model = request.Model,
            Messages = new List<ChatMessage>
            {
                new ChatMessage
                {
                    Role = "user",
                    Content = request.Prompt
                }
            },
            MaxTokens = request.MaxTokens,
            Temperature = request.Temperature,
            TopP = request.TopP,
            Seed = request.Seed,
            Stream = false
        };

        var chatResponse = await CreateChatCompletionAsync(chatRequest, cancellationToken);

        return new CompletionResponse
        {
            Id = chatResponse.Id,
            Model = chatResponse.Model,
            Text = chatResponse.Choices.FirstOrDefault()?.Message?.Content ?? "",
            FinishReason = chatResponse.Choices.FirstOrDefault()?.FinishReason,
            Usage = chatResponse.Usage,
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(chatResponse.Created)
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CompletionChunk> GenerateCompletionStreamAsync(
        CompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Convert to chat completion for backward compatibility
        var chatRequest = new ChatCompletionRequest
        {
            Model = request.Model,
            Messages = new List<ChatMessage>
            {
                new ChatMessage
                {
                    Role = "user",
                    Content = request.Prompt
                }
            },
            MaxTokens = request.MaxTokens,
            Temperature = request.Temperature,
            TopP = request.TopP,
            Seed = request.Seed,
            Stream = true
        };

        await foreach (var chunk in CreateChatCompletionStreamAsync(chatRequest, cancellationToken))
        {
            var choice = chunk.Choices.FirstOrDefault();
            if (choice != null)
            {
                yield return new CompletionChunk
                {
                    Text = choice.Delta?.Content ?? "",
                    IsFinished = choice.FinishReason != null,
                    FinishReason = choice.FinishReason
                };
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Model>> ListModelsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _modelsService.ListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Model> GetModelAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        return await _modelsService.RetrieveAsync(modelId, cancellationToken);
    }
}