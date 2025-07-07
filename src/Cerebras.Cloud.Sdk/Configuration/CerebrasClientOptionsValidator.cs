using System;
using Microsoft.Extensions.Options;

namespace Cerebras.Cloud.Sdk.Configuration;

/// <summary>
/// Validator for Cerebras client options.
/// </summary>
internal class CerebrasClientOptionsValidator : IValidateOptions<CerebrasClientOptions>
{
    public ValidateOptionsResult Validate(string? name, CerebrasClientOptions options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("Options cannot be null");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return ValidateOptionsResult.Fail("BaseUrl is required");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("BaseUrl must be a valid URL");
        }

        if (string.IsNullOrWhiteSpace(options.DefaultModel))
        {
            return ValidateOptionsResult.Fail("DefaultModel is required");
        }

        if (options.TimeoutSeconds <= 0)
        {
            return ValidateOptionsResult.Fail("TimeoutSeconds must be greater than 0");
        }

        if (options.MaxRetries < 0)
        {
            return ValidateOptionsResult.Fail("MaxRetries cannot be negative");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey) && 
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CEREBRAS_API_KEY")))
        {
            return ValidateOptionsResult.Fail("ApiKey is required. Set it in configuration or via CEREBRAS_API_KEY environment variable.");
        }

        return ValidateOptionsResult.Success;
    }
}