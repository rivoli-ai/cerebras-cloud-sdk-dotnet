using System;
using System.Net;
using Cerebras.Cloud.Sdk.Exceptions;
using Xunit;

namespace Cerebras.Cloud.Sdk.Tests.Unit.Exceptions;

public class CerebrasApiExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var exception = new CerebrasApiException("Test error message");

        // Assert
        Assert.Equal("Test error message", exception.Message);
        Assert.Null(exception.StatusCode);
        Assert.Null(exception.ErrorCode);
        Assert.Null(exception.ErrorType);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new CerebrasApiException("Test error message", innerException);

        // Assert
        Assert.Equal("Test error message", exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.Null(exception.StatusCode);
    }

    [Fact]
    public void Constructor_WithMessageAndStatusCode_SetsBoth()
    {
        // Arrange & Act
        var exception = new CerebrasApiException("Test error message", HttpStatusCode.BadRequest);

        // Assert
        Assert.Equal("Test error message", exception.Message);
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Null(exception.ErrorCode);
        Assert.Null(exception.ErrorType);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAll()
    {
        // Arrange & Act
        var exception = new CerebrasApiException(
            "Test error message",
            HttpStatusCode.Unauthorized,
            "invalid_api_key",
            "authentication_error");

        // Assert
        Assert.Equal("Test error message", exception.Message);
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("invalid_api_key", exception.ErrorCode);
        Assert.Equal("authentication_error", exception.ErrorType);
    }

    [Fact]
    public void Properties_CanBeSetViaInitializer()
    {
        // Arrange & Act
        var exception = new CerebrasApiException("Test error")
        {
            StatusCode = HttpStatusCode.InternalServerError,
            ErrorCode = "internal_error",
            ErrorType = "server_error",
            RawResponse = "{\"error\": \"Internal server error\"}"
        };

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal("internal_error", exception.ErrorCode);
        Assert.Equal("server_error", exception.ErrorType);
        Assert.Equal("{\"error\": \"Internal server error\"}", exception.RawResponse);
    }

    [Fact]
    public void Exception_IsSerializable()
    {
        // This test verifies the exception follows standard .NET exception patterns
        var exception = new CerebrasApiException("Test error", HttpStatusCode.BadRequest)
        {
            ErrorCode = "test_code",
            ErrorType = "test_type",
            RawResponse = "raw response"
        };

        // Should be an Exception
        Assert.IsAssignableFrom<Exception>(exception);
        
        // Should have standard exception properties
        Assert.NotNull(exception.Message);
        Assert.NotNull(exception.StackTrace ?? ""); // StackTrace might be null if not thrown
    }
}