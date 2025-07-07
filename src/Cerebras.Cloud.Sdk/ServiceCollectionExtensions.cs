using System;
using Cerebras.Cloud.Sdk.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cerebras.Cloud.Sdk;

/// <summary>
/// Extension methods for configuring Cerebras client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Cerebras client services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCerebrasClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        // Configure options
        services.Configure<CerebrasClientOptions>(
            configuration.GetSection(CerebrasClientOptions.SectionName));

        // Add options validation
        services.AddSingleton<IValidateOptions<CerebrasClientOptions>, CerebrasClientOptionsValidator>();

        // Configure HTTP client
        services.AddHttpClient<CerebrasClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<CerebrasClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "Cerebras.Cloud.Sdk/1.0");
        });

        // Register the client interface
        services.AddTransient<ICerebrasClient, CerebrasClient>();

        return services;
    }

    /// <summary>
    /// Adds Cerebras client services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The options configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCerebrasClient(
        this IServiceCollection services,
        Action<CerebrasClientOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        // Configure options
        services.Configure(configureOptions);

        // Add options validation
        services.AddSingleton<IValidateOptions<CerebrasClientOptions>, CerebrasClientOptionsValidator>();

        // Configure HTTP client
        services.AddHttpClient<CerebrasClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<CerebrasClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "Cerebras.Cloud.Sdk/1.0");
        });

        // Register the client interface
        services.AddTransient<ICerebrasClient, CerebrasClient>();

        return services;
    }
}

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