using System;
using System.Net;

namespace Cerebras.Cloud.Sdk.Exceptions;

/// <summary>
/// Exception thrown when an error occurs while interacting with the Cerebras API.
/// </summary>
public class CerebrasApiException : Exception
{
    /// <summary>
    /// Gets the HTTP status code associated with the error, if available.
    /// </summary>
    public HttpStatusCode? StatusCode { get; init; }

    /// <summary>
    /// Gets the error code from the API response, if available.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets the error type from the API response, if available.
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    /// Gets the raw response content from the API, if available.
    /// </summary>
    public string? RawResponse { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CerebrasApiException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CerebrasApiException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CerebrasApiException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CerebrasApiException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CerebrasApiException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public CerebrasApiException(string message, HttpStatusCode statusCode) 
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CerebrasApiException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorCode">The error code from the API.</param>
    /// <param name="errorType">The error type from the API.</param>
    public CerebrasApiException(string message, HttpStatusCode statusCode, string errorCode, string errorType) 
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorType = errorType;
    }
}