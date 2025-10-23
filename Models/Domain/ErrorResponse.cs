namespace BoltWebAPI.Models.Domain;

/// <summary>
/// Standard error response model
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Validation errors (field -> error messages)
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    /// <summary>
    /// Trace ID for debugging
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Additional error data
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }
}
