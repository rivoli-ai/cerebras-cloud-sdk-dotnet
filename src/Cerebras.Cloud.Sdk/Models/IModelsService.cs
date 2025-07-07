using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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