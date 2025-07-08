using System;
using Cerebras.Cloud.Sdk;
using Cerebras.Cloud.Sdk.Chat;
using Cerebras.Cloud.Sdk.Completions;
using Cerebras.Cloud.Sdk.Configuration;
using Cerebras.Cloud.Sdk.Http;
using Cerebras.Cloud.Sdk.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCerebrasClient_WithConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("CerebrasClient:ApiKey", "test-key"),
                new KeyValuePair<string, string?>("CerebrasClient:BaseUrl", "https://api.cerebras.ai/v1/"),
                new KeyValuePair<string, string?>("CerebrasClient:DefaultModel", "llama3.1-70b")
            })
            .Build();

        // Act
        services.AddCerebrasClient(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var client = provider.GetService<ICerebrasClient>();
        Assert.NotNull(client);
        Assert.IsType<CerebrasClient>(client);

        var options = provider.GetService<IOptions<CerebrasClientOptions>>();
        Assert.NotNull(options);
        Assert.Equal("test-key", options.Value.ApiKey);
    }

    [Fact]
    public void AddCerebrasClient_WithAction_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCerebrasClient(options =>
        {
            options.ApiKey = "test-key";
            options.BaseUrl = "https://api.cerebras.ai/v1/";
            options.DefaultModel = "llama3.1-70b";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var client = provider.GetService<ICerebrasClient>();
        Assert.NotNull(client);
        Assert.IsType<CerebrasClient>(client);

        var options = provider.GetService<IOptions<CerebrasClientOptions>>();
        Assert.NotNull(options);
        Assert.Equal("test-key", options.Value.ApiKey);
    }

    [Fact]
    public void AddCerebrasClient_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddCerebrasClient(configuration));
        Assert.Throws<ArgumentNullException>(() => services!.AddCerebrasClient(options => { }));
    }

    [Fact]
    public void AddCerebrasClient_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration? configuration = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddCerebrasClient(configuration!));
    }

    [Fact]
    public void AddCerebrasClient_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<CerebrasClientOptions>? configureOptions = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddCerebrasClient(configureOptions!));
    }

    [Fact]
    public void AddCerebrasClientV2_WithConfiguration_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("CerebrasClient:ApiKey", "test-key"),
                new KeyValuePair<string, string?>("CerebrasClient:BaseUrl", "https://api.cerebras.ai/v1/"),
                new KeyValuePair<string, string?>("CerebrasClient:DefaultModel", "llama3.1-70b")
            })
            .Build();

        services.AddLogging(); // Required for services

        // Act
        services.AddCerebrasClientV2(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        // Check V2 client
        var clientV2 = provider.GetService<ICerebrasClientV2>();
        Assert.NotNull(clientV2);
        Assert.IsType<CerebrasClientV2>(clientV2);

        // Check backward compatibility
        var clientV1 = provider.GetService<ICerebrasClient>();
        Assert.NotNull(clientV1);

        // Check services are registered
        var httpService = provider.GetService<IHttpService>();
        Assert.NotNull(httpService);

        var chatService = provider.GetService<IChatCompletionService>();
        Assert.NotNull(chatService);

        var completionService = provider.GetService<ICompletionService>();
        Assert.NotNull(completionService);

        var modelsService = provider.GetService<IModelsService>();
        Assert.NotNull(modelsService);
    }

    [Fact]
    public void AddCerebrasClientV2_WithAction_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCerebrasClientV2(options =>
        {
            options.ApiKey = "test-key";
            options.BaseUrl = "https://api.cerebras.ai/v1/";
            options.DefaultModel = "llama3.1-70b";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var client = provider.GetService<ICerebrasClientV2>();
        Assert.NotNull(client);
        Assert.IsType<CerebrasClientV2>(client);
    }

    [Fact]
    public void CerebrasClientOptionsValidator_ValidatesCorrectly()
    {
        // Arrange
        var validator = new CerebrasClientOptionsValidator();
        Environment.SetEnvironmentVariable("CEREBRAS_API_KEY", "test-key");

        try
        {
            // Test valid options
            var validOptions = new CerebrasClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://api.cerebras.ai/v1/",
                DefaultModel = "llama3.1-70b",
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            var result = validator.Validate(null, validOptions);
            Assert.True(result.Succeeded);

            // Test null options
            result = validator.Validate(null, null!);
            Assert.False(result.Succeeded);
            Assert.Equal("Options cannot be null", result.FailureMessage);

            // Test missing base URL
            var invalidOptions = new CerebrasClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = "",
                DefaultModel = "model"
            };
            result = validator.Validate(null, invalidOptions);
            Assert.False(result.Succeeded);
            Assert.Contains("BaseUrl is required", result.FailureMessage);

            // Test invalid URL
            invalidOptions.BaseUrl = "not-a-url";
            result = validator.Validate(null, invalidOptions);
            Assert.False(result.Succeeded);
            Assert.Contains("BaseUrl must be a valid URL", result.FailureMessage);

            // Test missing model
            invalidOptions = new CerebrasClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://api.cerebras.ai/v1/",
                DefaultModel = ""
            };
            result = validator.Validate(null, invalidOptions);
            Assert.False(result.Succeeded);
            Assert.Contains("DefaultModel is required", result.FailureMessage);

            // Test invalid timeout
            invalidOptions = new CerebrasClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://api.cerebras.ai/v1/",
                DefaultModel = "model",
                TimeoutSeconds = 0
            };
            result = validator.Validate(null, invalidOptions);
            Assert.False(result.Succeeded);
            Assert.Contains("TimeoutSeconds must be greater than 0", result.FailureMessage);

            // Test negative retries
            invalidOptions = new CerebrasClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://api.cerebras.ai/v1/",
                DefaultModel = "model",
                TimeoutSeconds = 30,
                MaxRetries = -1
            };
            result = validator.Validate(null, invalidOptions);
            Assert.False(result.Succeeded);
            Assert.Contains("MaxRetries cannot be negative", result.FailureMessage);

            // API key validation is now done at runtime in the services
            // The validator no longer checks for API key to allow environment variable usage
        }
        finally
        {
            Environment.SetEnvironmentVariable("CEREBRAS_API_KEY", null);
        }
    }
}