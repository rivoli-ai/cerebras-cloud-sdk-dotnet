using System.ComponentModel.DataAnnotations;

namespace Cerebras.Cloud.Sdk.Configuration;

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