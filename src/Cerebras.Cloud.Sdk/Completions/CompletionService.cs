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

namespace Cerebras.Cloud.Sdk.Completions;

/// <summary>
/// Implementation of text completion service.
/// </summary>
internal class CompletionService : ICompletionService
{
    private readonly IHttpService _httpService;
    private readonly ILogger<CompletionService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CompletionService(
        IHttpService httpService,
        ILogger<CompletionService> logger)
    {
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<TextCompletionResponse> CreateAsync(
        TextCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogDebug("Creating text completion with model {Model}", request.Model);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "completions");
        
        // Ensure stream is false for non-streaming requests
        var requestData = request with { Stream = false };
        httpRequest.Content = JsonContent.Create(requestData, options: _jsonOptions);

        var response = await _httpService.SendAsync(httpRequest, cancellationToken);

        var completionResponse = await response.Content.ReadFromJsonAsync<TextCompletionResponse>(
            _jsonOptions, cancellationToken);

        if (completionResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize text completion response");
        }

        _logger.LogInformation(
            "Text completion created successfully. Model: {Model}, Tokens: {Tokens}",
            completionResponse.Model,
            completionResponse.Usage?.TotalTokens ?? 0);

        return completionResponse;
    }

    public async IAsyncEnumerable<TextCompletionChunk> CreateStreamAsync(
        TextCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogDebug("Creating streaming text completion with model {Model}", request.Model);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "completions");
        
        // Ensure stream is true for streaming requests
        var requestData = request with { Stream = true };
        httpRequest.Content = JsonContent.Create(requestData, options: _jsonOptions);

        await foreach (var chunk in _httpService.SendStreamAsync<TextCompletionChunk>(
            httpRequest, _jsonOptions, cancellationToken))
        {
            yield return chunk;
        }
    }
}