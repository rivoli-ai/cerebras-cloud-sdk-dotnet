using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Cerebras.Cloud.Sdk.Http;

/// <summary>
/// HTTP service interface for making API requests.
/// </summary>
internal interface IHttpService
{
    /// <summary>
    /// Sends an HTTP request.
    /// </summary>
    Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an HTTP request and returns a streaming response.
    /// </summary>
    IAsyncEnumerable<T> SendStreamAsync<T>(
        HttpRequestMessage request,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken = default);
}