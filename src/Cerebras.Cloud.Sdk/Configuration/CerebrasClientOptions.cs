using System.ComponentModel.DataAnnotations;

namespace Cerebras.Cloud.Sdk.Configuration;

/// <summary>
/// Configuration options for the Cerebras client.
/// </summary>
public class CerebrasClientOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "CerebrasClient";

    /// <summary>
    /// The API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The base URL for the Cerebras API.
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://api.cerebras.ai/v1/";

    /// <summary>
    /// The default model to use.
    /// </summary>
    [Required]
    public string DefaultModel { get; set; } = "llama3.1-70b";

    /// <summary>
    /// The default temperature for generation.
    /// </summary>
    [Range(0.0, 2.0)]
    public double DefaultTemperature { get; set; } = 0.7;

    /// <summary>
    /// The default maximum tokens for generation.
    /// </summary>
    [Range(1, 8192)]
    public int DefaultMaxTokens { get; set; } = 1024;

    /// <summary>
    /// The timeout for API requests in seconds.
    /// </summary>
    [Range(1, 600)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// The maximum number of retries for failed requests.
    /// </summary>
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Whether to enable request/response logging.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Whether to enable response caching.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// The cache duration in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Rate limiting configuration.
    /// </summary>
    public RateLimitOptions RateLimit { get; set; } = new();
}

/// <summary>
/// Rate limiting options for the Cerebras client.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The maximum number of requests per minute.
    /// </summary>
    [Range(1, 1000)]
    public int RequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// The maximum number of tokens per minute.
    /// </summary>
    [Range(1000, 1000000)]
    public int TokensPerMinute { get; set; } = 100000;
}