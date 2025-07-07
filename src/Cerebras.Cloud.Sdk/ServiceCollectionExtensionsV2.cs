using System;
using Cerebras.Cloud.Sdk.Chat;
using Cerebras.Cloud.Sdk.Completions;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Http;
using Cerebras.Cloud.Sdk.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cerebras.Cloud.Sdk;

/// <summary>
/// Extension methods for configuring enhanced Cerebras client services.
/// </summary>
public static class ServiceCollectionExtensionsV2
{
    /// <summary>
    /// Adds enhanced Cerebras client services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCerebrasClientV2(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Configure options
        services.Configure<CerebrasClientOptions>(
            configuration.GetSection(CerebrasClientOptions.SectionName));

        return AddCerebrasClientCore(services);
    }

    /// <summary>
    /// Adds enhanced Cerebras client services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The options configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCerebrasClientV2(
        this IServiceCollection services,
        Action<CerebrasClientOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // Configure options
        services.Configure(configureOptions);

        return AddCerebrasClientCore(services);
    }

    private static IServiceCollection AddCerebrasClientCore(IServiceCollection services)
    {
        // Add options validation
        services.AddSingleton<IValidateOptions<CerebrasClientOptions>, CerebrasClientOptionsValidator>();

        // Configure HTTP client for HttpService
        services.AddHttpClient<IHttpService, HttpService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<CerebrasClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        // Register services
        services.AddTransient<IHttpService, HttpService>();
        services.AddTransient<IChatCompletionService, ChatCompletionService>();
        services.AddTransient<ICompletionService, CompletionService>();
        services.AddTransient<IModelsService, ModelsService>();

        // Configure main HTTP client for CerebrasClient (backward compatibility)
        services.AddHttpClient<CerebrasClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<CerebrasClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        // Register both interfaces
        services.AddTransient<ICerebrasClient, CerebrasClient>();
        services.AddTransient<ICerebrasClientV2, CerebrasClientV2>();

        return services;
    }
}