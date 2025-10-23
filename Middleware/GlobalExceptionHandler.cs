using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using BoltWebAPI.Models.Domain;

namespace BoltWebAPI.Middleware;

/// <summary>
/// Global exception handler for catching and formatting unhandled exceptions
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception,
            "Unhandled exception occurred: {Message} (Path: {Path}, Method: {Method})",
            exception.Message,
            httpContext.Request.Path,
            httpContext.Request.Method);

        var (statusCode, errorCode, message) = MapException(exception);

        var errorResponse = new ErrorResponse
        {
            Message = message,
            ErrorCode = errorCode
        };

        // In development, include stack trace for debugging
        if (_environment.IsDevelopment())
        {
            errorResponse.Data = new Dictionary<string, object>
            {
                { "exceptionType", exception.GetType().Name },
                { "stackTrace", exception.StackTrace ?? string.Empty },
                { "innerException", exception.InnerException?.Message ?? string.Empty }
            };
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await httpContext.Response.WriteAsJsonAsync(errorResponse, options, cancellationToken);

        return true;
    }

    private (int statusCode, string errorCode, string message) MapException(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => (
                StatusCodes.Status400BadRequest,
                "MISSING_ARGUMENT",
                exception.Message
            ),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "INVALID_ARGUMENT",
                exception.Message
            ),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "RESOURCE_NOT_FOUND",
                exception.Message
            ),
            FileNotFoundException => (
                StatusCodes.Status404NotFound,
                "FILE_NOT_FOUND",
                exception.Message
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "ACCESS_DENIED",
                "You do not have permission to access this resource"
            ),
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "INVALID_OPERATION",
                exception.Message
            ),
            TimeoutException => (
                StatusCodes.Status408RequestTimeout,
                "TIMEOUT",
                "The operation timed out"
            ),
            NotImplementedException => (
                StatusCodes.Status501NotImplemented,
                "NOT_IMPLEMENTED",
                "This feature is not yet implemented"
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "INTERNAL_SERVER_ERROR",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later."
            )
        };
    }
}
