using System.Threading.RateLimiting;

namespace BoltWebAPI.Middleware;

/// <summary>
/// Rate limiting policy configurations
/// </summary>
public static class RateLimitingPolicies
{
    public const string Default = "DefaultPolicy";
    public const string Execution = "ExecutionPolicy";
    public const string Authentication = "AuthenticationPolicy";
    public const string HistoryQuery = "HistoryQueryPolicy";

    /// <summary>
    /// Configures rate limiting policies
    /// </summary>
    public static IServiceCollection AddRateLimitingPolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // Default policy: 100 requests per minute per user
            options.AddPolicy(Default, httpContext =>
            {
                var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue("RateLimiting:Default:PermitLimit", 100),
                        Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Default:WindowMinutes", 1)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = configuration.GetValue("RateLimiting:Default:QueueLimit", 5)
                    });
            });

            // Execution policy: 10 executions per minute per user (more restrictive)
            options.AddPolicy(Execution, httpContext =>
            {
                var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue("RateLimiting:Execution:PermitLimit", 10),
                        Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Execution:WindowMinutes", 1)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queue for execution requests
                    });
            });

            // Authentication policy: 5 login attempts per minute per IP
            options.AddPolicy(Authentication, httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue("RateLimiting:Authentication:PermitLimit", 5),
                        Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Authentication:WindowMinutes", 1)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queue for auth requests
                    });
            });

            // History query policy: 30 queries per minute per user
            options.AddPolicy(HistoryQuery, httpContext =>
            {
                var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue("RateLimiting:HistoryQuery:PermitLimit", 30),
                        Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:HistoryQuery:WindowMinutes", 1)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    });
            });

            // Global rate limiter
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: "global",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue("RateLimiting:Global:PermitLimit", 1000),
                        Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Global:WindowMinutes", 1)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // Rejection response
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                TimeSpan? retryAfter = null;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
                {
                    retryAfter = retryAfterValue;
                    context.HttpContext.Response.Headers.RetryAfter = retryAfterValue.TotalSeconds.ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    message = "Too many requests. Please try again later.",
                    errorCode = "RATE_LIMIT_EXCEEDED",
                    retryAfterSeconds = retryAfter?.TotalSeconds
                }, cancellationToken: cancellationToken);
            };
        });

        return services;
    }
}
