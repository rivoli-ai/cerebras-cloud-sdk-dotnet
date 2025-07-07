using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cerebras.Cloud.Sdk.Http;
using Microsoft.Extensions.Logging;

namespace Cerebras.Cloud.Sdk.Models;

/// <summary>
/// Service for managing models.
/// </summary>
public interface IModelsService
{
    /// <summary>
    /// Lists available models.
    /// </summary>
    Task<IReadOnlyList<Model>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific model.
    /// </summary>
    Task<Model> RetrieveAsync(string modelId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of models service.
/// </summary>
internal class ModelsService : IModelsService
{
    private readonly IHttpService _httpService;
    private readonly ILogger<ModelsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ModelsService(
        IHttpService httpService,
        ILogger<ModelsService> logger)
    {
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public async Task<IReadOnlyList<Model>> ListAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing available models");

        var request = new HttpRequestMessage(HttpMethod.Get, "models");
        var response = await _httpService.SendAsync(request, cancellationToken);

        var modelsResponse = await response.Content.ReadFromJsonAsync<CerebrasModelsResponse>(
            _jsonOptions, cancellationToken);

        if (modelsResponse?.Data == null)
        {
            throw new InvalidOperationException("Failed to parse models response");
        }

        var models = modelsResponse.Data.Select(m => new Model
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

    public async Task<Model> RetrieveAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID is required", nameof(modelId));

        _logger.LogDebug("Retrieving model {ModelId}", modelId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"models/{modelId}");
        var response = await _httpService.SendAsync(request, cancellationToken);

        var modelResponse = await response.Content.ReadFromJsonAsync<CerebrasModelsResponse.ModelInfo>(
            _jsonOptions, cancellationToken);

        if (modelResponse == null || string.IsNullOrEmpty(modelResponse.Id))
        {
            throw new InvalidOperationException($"Failed to parse model response for '{modelId}'");
        }

        return new Model
        {
            Id = modelResponse.Id,
            Name = modelResponse.Name ?? modelResponse.Id,
            Description = modelResponse.Description,
            ContextWindow = modelResponse.ContextLength,
            IsAvailable = modelResponse.IsAvailable ?? true
        };
    }
}