using BoltWebAPI.Models.Domain;

namespace BoltWebAPI.Services.Interfaces;

/// <summary>
/// Service for querying and managing execution history
/// </summary>
public interface IExecutionHistoryService
{
    /// <summary>
    /// Gets paginated execution history with optional filters
    /// </summary>
    /// <param name="query">Query parameters including filters, pagination, and sorting</param>
    /// <returns>Paginated execution history response</returns>
    Task<ExecutionHistoryResponse> GetExecutionHistoryAsync(ExecutionHistoryQuery query);

    /// <summary>
    /// Gets a specific execution record by ID
    /// </summary>
    /// <param name="executionId">Execution unique identifier</param>
    /// <returns>Execution history record</returns>
    /// <exception cref="KeyNotFoundException">Thrown when execution is not found</exception>
    Task<ExecutionHistoryRecord> GetExecutionByIdAsync(Guid executionId);

    /// <summary>
    /// Gets execution statistics for a specific time period
    /// </summary>
    /// <param name="periodFrom">Start of period (null for all time)</param>
    /// <param name="periodTo">End of period (null for now)</param>
    /// <param name="userId">Optional filter by user ID</param>
    /// <returns>Execution statistics summary</returns>
    Task<ExecutionStatistics> GetExecutionStatisticsAsync(
        DateTime? periodFrom = null,
        DateTime? periodTo = null,
        string? userId = null);

    /// <summary>
    /// Deletes old execution history records
    /// </summary>
    /// <param name="olderThan">Delete records older than this date</param>
    /// <param name="userId">User performing the operation</param>
    /// <returns>Number of records deleted</returns>
    Task<int> DeleteOldExecutionsAsync(DateTime olderThan, string userId);

    /// <summary>
    /// Deletes a specific execution record
    /// </summary>
    /// <param name="executionId">Execution ID to delete</param>
    /// <param name="userId">User performing the operation</param>
    /// <exception cref="KeyNotFoundException">Thrown when execution is not found</exception>
    Task DeleteExecutionAsync(Guid executionId, string userId);
}
