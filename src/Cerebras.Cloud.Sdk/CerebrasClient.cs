using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Models;
using Cerebras.Cloud.Sdk.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Cerebras.Cloud.Sdk;

/// <summary>
/// Client for interacting with the Cerebras AI API.
/// </summary>
public class CerebrasClient : ICerebrasClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CerebrasClient> _logger;
    private readonly CerebrasClientOptions _options;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CerebrasClient"/> class.
    /// </summary>
    public CerebrasClient(
        HttpClient httpClient,
        ILogger<CerebrasClient> logger,
        IOptions<CerebrasClientOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // If API key is not set in options, try to get it from environment variable
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _options.ApiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");
        }

        // Ensure base address is set
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

        // Configure retry policy
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && r.StatusCode != HttpStatusCode.BadRequest)
            .WaitAndRetryAsync(
                _options.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} after {Delay}ms due to {StatusCode}",
                        retryCount, timespan.TotalMilliseconds, outcome.Result?.StatusCode);
                });

        // Configure JSON options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<CompletionResponse> GenerateCompletionAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating completion with model {Model}", request.Model);

            // Ensure API key is set
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("API key is not configured. Please set the ApiKey in CerebrasClientOptions.");
            }

            var requestBody = new
            {
                model = request.Model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = request.Prompt
                    }
                },
                max_completion_tokens = request.MaxTokens ?? _options.DefaultMaxTokens,
                temperature = request.Temperature ?? _options.DefaultTemperature,
                top_p = request.TopP ?? 1.0,
                seed = request.Seed ?? 0,
                stream = false
            };

            // Log the request for debugging
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var requestBodyJson = JsonSerializer.Serialize(requestBody, _jsonOptions);
                _logger.LogDebug("Sending request to Cerebras API: {RequestBody}", requestBodyJson);
            }

            // Send request with retry
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
                httpRequest.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");
                httpRequest.Content = JsonContent.Create(requestBody, options: _jsonOptions);
                return await _httpClient.SendAsync(httpRequest, cancellationToken);
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Cerebras API request failed with status {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                throw new CerebrasApiException($"Request failed with status {response.StatusCode}: {errorContent}", response.StatusCode);
            }

            // Parse response
            var responseData = await response.Content.ReadFromJsonAsync<CerebrasCompletionResponse>(
                _jsonOptions, cancellationToken);

            if (responseData == null)
            {
                throw new CerebrasApiException("Failed to parse response from Cerebras API");
            }

            var completionResponse = new CompletionResponse
            {
                Id = responseData.Id,
                Model = responseData.Model,
                Text = responseData.Choices?.FirstOrDefault()?.Message?.Content ?? "",
                FinishReason = responseData.Choices?.FirstOrDefault()?.FinishReason,
                Usage = responseData.Usage != null
                    ? new Usage
                    {
                        PromptTokens = responseData.Usage.PromptTokens,
                        CompletionTokens = responseData.Usage.CompletionTokens
                    }
                    : null,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(responseData.Created)
            };

            _logger.LogInformation(
                "Successfully generated completion. Tokens: {TotalTokens}",
                completionResponse.Usage?.TotalTokens ?? 0);

            return completionResponse;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Completion request was cancelled");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed");
            throw new CerebrasApiException("Network error occurred", ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (CerebrasApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during completion generation");
            throw new CerebrasApiException("An unexpected error occurred", ex);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CompletionChunk> GenerateCompletionStreamAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating streaming completion with model {Model}", request.Model);

        // Ensure API key is set
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("API key is not configured. Please set the ApiKey in CerebrasClientOptions.");
        }

        // Prepare request
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        httpRequest.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");

        var requestBody = new
        {
            model = request.Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = request.Prompt
                }
            },
            max_completion_tokens = request.MaxTokens ?? _options.DefaultMaxTokens,
            temperature = request.Temperature ?? _options.DefaultTemperature,
            top_p = request.TopP ?? 1.0,
            seed = request.Seed ?? 0,
            stream = true
        };

        httpRequest.Content = JsonContent.Create(requestBody, options: _jsonOptions);

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new CerebrasApiException($"Request failed with status {response.StatusCode}: {errorContent}", response.StatusCode);
            }

            // Read streaming response
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!line.StartsWith("data: "))
                {
                    continue;
                }

                var data = line.Substring(6);
                if (data == "[DONE]")
                {
                    yield return new CompletionChunk
                    {
                        Text = "",
                        IsFinished = true,
                        FinishReason = "stop"
                    };
                    break;
                }

                CerebrasStreamChunk? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<CerebrasStreamChunk>(data, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming chunk: {Data}", data);
                    continue;
                }

                var choice = chunk?.Choices?.FirstOrDefault();
                if (choice?.Delta?.Content != null)
                {
                    yield return new CompletionChunk
                    {
                        Text = choice.Delta.Content,
                        IsFinished = false,
                        FinishReason = choice.FinishReason
                    };
                }
            }
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Model>> ListModelsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Listing available Cerebras models");

            // Ensure API key is set
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("API key is not configured. Please set the ApiKey in CerebrasClientOptions.");
            }

            // Send request with retry
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, "models");
                httpRequest.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");
                return await _httpClient.SendAsync(httpRequest, cancellationToken);
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to list models. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new CerebrasApiException($"Request failed with status {response.StatusCode}: {errorContent}", response.StatusCode);
            }

            // Parse response
            var responseData = await response.Content.ReadFromJsonAsync<CerebrasModelsResponse>(
                _jsonOptions, cancellationToken);

            if (responseData?.Data == null)
            {
                throw new CerebrasApiException("Failed to parse models response");
            }

            var models = responseData.Data.Select(m => new Model
            {
                Id = m.Id,
                Name = m.Name ?? m.Id,
                Description = m.Description,
                ContextWindow = m.ContextLength,
                IsAvailable = m.IsAvailable ?? true
            }).ToList();

            _logger.LogInformation("Retrieved {Count} models", models.Count);
            return models;
        }
        catch (CerebrasApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list models");
            throw new CerebrasApiException("Failed to list models", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Model> GetModelAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID is required", nameof(modelId));
        }

        try
        {
            _logger.LogDebug("Getting model information for {ModelId}", modelId);

            // For now, we'll get all models and filter
            // In a real implementation, there might be a specific endpoint
            var models = await ListModelsAsync(cancellationToken);
            
            var model = models.FirstOrDefault(m => m.Id == modelId);
            if (model == null)
            {
                throw new CerebrasApiException($"Model '{modelId}' not found", HttpStatusCode.NotFound);
            }

            return model;
        }
        catch (CerebrasApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model {ModelId}", modelId);
            throw new CerebrasApiException("Failed to get model information", ex);
        }
    }
}