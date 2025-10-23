using System.Security.Claims;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BoltWebAPI.Endpoints;

/// <summary>
/// Endpoints for execution history management
/// </summary>
public static class ExecutionHistoryEndpoints
{
    /// <summary>
    /// Maps all execution history endpoints
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/history")
            .RequireAuthorization()
            .WithTags("Execution History");

        group.MapGet("/", GetExecutionHistory)
            .WithName("GetExecutionHistory")
            .WithSummary("Get execution history")
            .WithDescription("Retrieves paginated execution history with optional filters and sorting")
            .Produces<ExecutionHistoryResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/{executionId:guid}", GetExecutionById)
            .WithName("GetExecutionById")
            .WithSummary("Get execution details")
            .WithDescription("Retrieves detailed information about a specific execution")
            .Produces<ExecutionHistoryRecord>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/statistics", GetStatistics)
            .WithName("GetExecutionStatistics")
            .WithSummary("Get execution statistics")
            .WithDescription("Retrieves execution statistics for a specific time period")
            .Produces<ExecutionStatistics>(StatusCodes.Status200OK);

        group.MapDelete("/cleanup", CleanupOldExecutions)
            .WithName("CleanupOldExecutions")
            .WithSummary("Delete old executions")
            .WithDescription("Deletes execution history records older than specified date")
            .Produces<CleanupResult>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{executionId:guid}", DeleteExecution)
            .WithName("DeleteExecution")
            .WithSummary("Delete execution record")
            .WithDescription("Deletes a specific execution history record")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<ExecutionHistoryResponse>, BadRequest<ErrorResponse>>>
        GetExecutionHistory(
            [AsParameters] ExecutionHistoryQuery query,
            IValidator<ExecutionHistoryQuery> validator,
            IExecutionHistoryService historyService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ExecutionHistoryEndpoints");

        var validationResult = await validator.ValidateAsync(query);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors
            });
        }

        try
        {
            var response = await historyService.GetExecutionHistoryAsync(query);

            logger.LogInformation(
                "Retrieved execution history: Page {PageNumber}/{TotalPages}, Records {RecordCount}/{TotalCount}",
                response.PageNumber, response.TotalPages, response.Records.Count, response.TotalCount);

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving execution history");
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "QUERY_ERROR"
            });
        }
    }

    private static async Task<Results<Ok<ExecutionHistoryRecord>, NotFound<ErrorResponse>>>
        GetExecutionById(
            Guid executionId,
            IExecutionHistoryService historyService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ExecutionHistoryEndpoints");

        try
        {
            var record = await historyService.GetExecutionByIdAsync(executionId);
            return TypedResults.Ok(record);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Execution not found: {ExecutionId}", executionId);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Execution with ID {executionId} not found",
                ErrorCode = "EXECUTION_NOT_FOUND"
            });
        }
    }

    private static async Task<Ok<ExecutionStatistics>>
        GetStatistics(
            DateTime? periodFrom,
            DateTime? periodTo,
            string? userId,
            IExecutionHistoryService historyService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ExecutionHistoryEndpoints");

        var statistics = await historyService.GetExecutionStatisticsAsync(periodFrom, periodTo, userId);

        logger.LogInformation(
            "Retrieved execution statistics: Total={Total}, Success={Success}, Failed={Failed}, SuccessRate={SuccessRate:F2}%",
            statistics.TotalExecutions, statistics.CompletedExecutions, statistics.FailedExecutions, statistics.SuccessRate);

        return TypedResults.Ok(statistics);
    }

    private static async Task<Results<Ok<CleanupResult>, BadRequest<ErrorResponse>>>
        CleanupOldExecutions(
            DateTime olderThan,
            IExecutionHistoryService historyService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ExecutionHistoryEndpoints");

        if (olderThan >= DateTime.UtcNow)
        {
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "olderThan date must be in the past",
                ErrorCode = "INVALID_DATE"
            });
        }

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var deletedCount = await historyService.DeleteOldExecutionsAsync(olderThan, userId);

            logger.LogInformation(
                "Cleaned up {DeletedCount} execution records older than {Date} (User: {UserId})",
                deletedCount, olderThan, userId);

            return TypedResults.Ok(new CleanupResult
            {
                DeletedCount = deletedCount,
                OlderThan = olderThan
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during cleanup");
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "CLEANUP_ERROR"
            });
        }
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>>
        DeleteExecution(
            Guid executionId,
            IExecutionHistoryService historyService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ExecutionHistoryEndpoints");

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await historyService.DeleteExecutionAsync(executionId, userId);

            logger.LogInformation("Deleted execution {ExecutionId} (User: {UserId})", executionId, userId);

            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Execution not found for deletion: {ExecutionId}", executionId);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Execution with ID {executionId} not found",
                ErrorCode = "EXECUTION_NOT_FOUND"
            });
        }
    }
}

/// <summary>
/// Result of cleanup operation
/// </summary>
public class CleanupResult
{
    /// <summary>
    /// Number of records deleted
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// Date threshold used for cleanup
    /// </summary>
    public DateTime OlderThan { get; set; }
}
