using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Http;
using Microsoft.Extensions.Logging;

namespace Cerebras.Cloud.Sdk.Chat;

/// <summary>
/// Implementation of chat completion service.
/// </summary>
internal class ChatCompletionService : IChatCompletionService
{
    private readonly IHttpService _httpService;
    private readonly ILogger<ChatCompletionService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChatCompletionService(
        IHttpService httpService,
        ILogger<ChatCompletionService> logger)
    {
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<ChatCompletionResponse> CreateAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogDebug("Creating chat completion with model {Model}", request.Model);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        
        // Ensure stream is false for non-streaming requests
        var requestData = request with { Stream = false };
        httpRequest.Content = JsonContent.Create(requestData, options: _jsonOptions);

        var response = await _httpService.SendAsync(httpRequest, cancellationToken);

        var chatResponse = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
            _jsonOptions, cancellationToken);

        if (chatResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize chat completion response");
        }

        _logger.LogInformation(
            "Chat completion created successfully. Model: {Model}, Tokens: {Tokens}",
            chatResponse.Model,
            chatResponse.Usage?.TotalTokens ?? 0);

        return chatResponse;
    }

    public async IAsyncEnumerable<ChatCompletionChunk> CreateStreamAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogDebug("Creating streaming chat completion with model {Model}", request.Model);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        
        // Ensure stream is true for streaming requests
        var requestData = request with { Stream = true };
        httpRequest.Content = JsonContent.Create(requestData, options: _jsonOptions);

        await foreach (var chunk in _httpService.SendStreamAsync<ChatCompletionChunk>(
            httpRequest, _jsonOptions, cancellationToken))
        {
            yield return chunk;
        }
    }
}