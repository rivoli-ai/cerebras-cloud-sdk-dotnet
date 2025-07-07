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