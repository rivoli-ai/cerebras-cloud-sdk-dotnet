using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Cerebras.Cloud.Sdk.Http;

/// <summary>
/// HTTP service implementation for making API requests.
/// </summary>
internal class HttpService : IHttpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpService> _logger;
    private readonly CerebrasClientOptions _options;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly string _userAgent;

    public HttpService(
        HttpClient httpClient,
        ILogger<HttpService> logger,
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

        // Build user agent with platform info
        _userAgent = BuildUserAgent();

        // Configure retry policy
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(ShouldRetry)
            .WaitAndRetryAsync(
                _options.MaxRetries,
                retryAttempt => GetRetryDelay(retryAttempt),
                onRetry: OnRetry);
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _retryPolicy.ExecuteAsync(
                async () => 
                {
                    // Clone the request for each retry attempt
                    var clonedRequest = CloneHttpRequestMessage(request);
                    AddCommonHeaders(clonedRequest);
                    return await _httpClient.SendAsync(clonedRequest, cancellationToken);
                });

            if (!response.IsSuccessStatusCode)
            {
                await ThrowApiException(response, cancellationToken);
            }

            return response;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request was cancelled");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed");
            throw new CerebrasApiException("Network error occurred", ex);
        }
    }

    public async IAsyncEnumerable<T> SendStreamAsync<T>(
        HttpRequestMessage request,
        JsonSerializerOptions jsonOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Add common headers
        AddCommonHeaders(request);

        HttpResponseMessage? response = null;
        Stream? stream = null;
        StreamReader? reader = null;

        try
        {
            response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowApiException(response, cancellationToken);
            }

            stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            reader = new StreamReader(stream);

            await foreach (var item in ReadServerSentEvents<T>(reader, jsonOptions, cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            reader?.Dispose();
            stream?.Dispose();
            response?.Dispose();
        }
    }

    private async IAsyncEnumerable<T> ReadServerSentEvents<T>(
        StreamReader reader,
        JsonSerializerOptions jsonOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
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
                yield break;
            }

            T? item = default;
            try
            {
                item = JsonSerializer.Deserialize<T>(data, jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse SSE data: {Data}", data);
                continue;
            }

            if (item != null)
            {
                yield return item;
            }
        }
    }

    private void AddCommonHeaders(HttpRequestMessage request)
    {
        // Add API key
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        // Add user agent
        request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

        // Add idempotency key if not present
        if (!request.Headers.Contains("X-Request-Id"))
        {
            request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
        }
    }

    private string BuildUserAgent()
    {
        var sdkVersion = typeof(HttpService).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        var runtime = Environment.Version.ToString();
        var os = Environment.OSVersion.Platform.ToString();
        var osVersion = Environment.OSVersion.Version.ToString();

        return $"Cerebras-DotNet-SDK/{sdkVersion} (.NET/{runtime}; {os}/{osVersion})";
    }

    private bool ShouldRetry(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return false;

        // Don't retry client errors except for specific cases
        if (response.StatusCode == HttpStatusCode.BadRequest)
            return false;

        // Retry on specific status codes
        return response.StatusCode switch
        {
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.Conflict => true,
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }

    private TimeSpan GetRetryDelay(int retryAttempt)
    {
        // Exponential backoff with jitter
        var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
        return baseDelay + jitter;
    }

    private void OnRetry(DelegateResult<HttpResponseMessage> outcome, TimeSpan delay, int retryCount, Context context)
    {
        var response = outcome.Result;
        if (response != null)
        {
            // Check for Retry-After header
            if (response.Headers.RetryAfter != null)
            {
                var retryAfter = response.Headers.RetryAfter.Delta ?? TimeSpan.FromSeconds(response.Headers.RetryAfter.Date?.Subtract(DateTimeOffset.UtcNow).TotalSeconds ?? 60);
                delay = retryAfter;
            }

            _logger.LogWarning(
                "Retry {RetryCount} after {Delay}ms due to {StatusCode}",
                retryCount, delay.TotalMilliseconds, response.StatusCode);
        }
    }

    private async Task ThrowApiException(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        string? errorMessage = null;
        string? errorType = null;
        string? errorCode = null;

        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            
            var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>(jsonOptions, cancellationToken);
            if (errorResponse?.Error != null)
            {
                errorMessage = errorResponse.Error.Message;
                errorType = errorResponse.Error.Type;
                errorCode = errorResponse.Error.Code;
            }
        }
        catch
        {
            // If we can't parse the error, use the raw content
            errorMessage = errorContent;
        }

        var message = errorMessage ?? $"Request failed with status {response.StatusCode}";

        _logger.LogError(
            "Cerebras API request failed with status {StatusCode}: {Error}",
            response.StatusCode, message);

        throw new CerebrasApiException(message, response.StatusCode)
        {
            ErrorType = errorType,
            ErrorCode = errorCode,
            RawResponse = errorContent
        };
    }

    private record ErrorResponse(ErrorInfo? Error);
    private record ErrorInfo(string Message, string? Type, string? Code, string? Param);

    private static HttpRequestMessage CloneHttpRequestMessage(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = request.Content,
            Version = request.Version
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var property in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(property.Key), property.Value);
        }

        return clone;
    }
}