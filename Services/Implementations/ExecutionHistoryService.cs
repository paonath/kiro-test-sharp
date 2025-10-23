using BoltWebAPI.Data;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Models.Entities;
using BoltWebAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoltWebAPI.Services.Implementations;

/// <summary>
/// Service for querying and managing execution history with pagination and filtering
/// </summary>
public class ExecutionHistoryService : IExecutionHistoryService
{
    private readonly BoltDbContext _dbContext;
    private readonly ILogger<ExecutionHistoryService> _logger;

    public ExecutionHistoryService(
        BoltDbContext dbContext,
        ILogger<ExecutionHistoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ExecutionHistoryResponse> GetExecutionHistoryAsync(ExecutionHistoryQuery query)
    {
        // Validate and normalize query parameters
        query.PageNumber = Math.Max(1, query.PageNumber);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);

        var queryable = _dbContext.ExecutionHistory.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(query.ExecutionType))
        {
            if (Enum.TryParse<ExecutionType>(query.ExecutionType, true, out var executionType))
            {
                queryable = queryable.Where(e => e.ExecutionType == executionType);
            }
        }

        if (!string.IsNullOrEmpty(query.State))
        {
            if (Enum.TryParse<Models.Entities.ExecutionState>(query.State, true, out var state))
            {
                queryable = queryable.Where(e => e.State == state);
            }
        }

        if (!string.IsNullOrEmpty(query.UserId))
        {
            queryable = queryable.Where(e => e.UserId == query.UserId);
        }

        if (!string.IsNullOrEmpty(query.ExecutionName))
        {
            queryable = queryable.Where(e => e.ExecutionName.Contains(query.ExecutionName));
        }

        if (query.StartDateFrom.HasValue)
        {
            queryable = queryable.Where(e => e.StartedAt >= query.StartDateFrom.Value);
        }

        if (query.StartDateTo.HasValue)
        {
            queryable = queryable.Where(e => e.StartedAt <= query.StartDateTo.Value);
        }

        // Get total count before pagination
        var totalCount = await queryable.CountAsync();

        // Apply sorting
        queryable = ApplySorting(queryable, query.SortBy, query.SortDirection);

        // Apply pagination
        var skip = (query.PageNumber - 1) * query.PageSize;
        var records = await queryable
            .Skip(skip)
            .Take(query.PageSize)
            .Select(e => new ExecutionHistoryRecord
            {
                ExecutionId = e.ExecutionId,
                ExecutionType = e.ExecutionType.ToString(),
                ExecutionName = e.ExecutionName,
                Targets = e.Targets ?? string.Empty,
                State = e.State.ToString(),
                ExitCode = e.ExitCode,
                StandardOutput = e.StandardOutput ?? string.Empty,
                StandardError = e.StandardError ?? string.Empty,
                UserId = e.UserId,
                Username = e.Username,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                DurationMs = e.DurationMs,
                ErrorMessage = e.ErrorMessage
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        var response = new ExecutionHistoryResponse
        {
            Records = records,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = totalPages,
            HasNextPage = query.PageNumber < totalPages,
            HasPreviousPage = query.PageNumber > 1
        };

        _logger.LogDebug(
            "Retrieved {RecordCount} execution history records (Page {PageNumber}/{TotalPages}, Total: {TotalCount})",
            records.Count, query.PageNumber, totalPages, totalCount);

        return response;
    }

    public async Task<ExecutionHistoryRecord> GetExecutionByIdAsync(Guid executionId)
    {
        var entity = await _dbContext.ExecutionHistory
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId);

        if (entity == null)
        {
            throw new KeyNotFoundException($"Execution with ID {executionId} not found");
        }

        var record = new ExecutionHistoryRecord
        {
            ExecutionId = entity.ExecutionId,
            ExecutionType = entity.ExecutionType.ToString(),
            ExecutionName = entity.ExecutionName,
            Targets = entity.Targets ?? string.Empty,
            State = entity.State.ToString(),
            ExitCode = entity.ExitCode,
            StandardOutput = entity.StandardOutput ?? string.Empty,
            StandardError = entity.StandardError ?? string.Empty,
            UserId = entity.UserId,
            Username = entity.Username,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            DurationMs = entity.DurationMs,
            ErrorMessage = entity.ErrorMessage
        };

        return record;
    }

    public async Task<ExecutionStatistics> GetExecutionStatisticsAsync(
        DateTime? periodFrom = null,
        DateTime? periodTo = null,
        string? userId = null)
    {
        var queryable = _dbContext.ExecutionHistory.AsQueryable();

        // Apply period filter
        if (periodFrom.HasValue)
        {
            queryable = queryable.Where(e => e.StartedAt >= periodFrom.Value);
        }

        if (periodTo.HasValue)
        {
            queryable = queryable.Where(e => e.StartedAt <= periodTo.Value);
        }

        // Apply user filter
        if (!string.IsNullOrEmpty(userId))
        {
            queryable = queryable.Where(e => e.UserId == userId);
        }

        var executions = await queryable.ToListAsync();

        var totalExecutions = executions.Count;
        var completedExecutions = executions.Count(e => e.State == Models.Entities.ExecutionState.Completed);
        var failedExecutions = executions.Count(e => e.State == Models.Entities.ExecutionState.Failed);
        var cancelledExecutions = executions.Count(e => e.State == Models.Entities.ExecutionState.Cancelled);
        var runningExecutions = executions.Count(e => e.State == Models.Entities.ExecutionState.Running);
        var queuedExecutions = executions.Count(e => e.State == Models.Entities.ExecutionState.Queued);

        var successRate = totalExecutions > 0
            ? (completedExecutions / (double)totalExecutions) * 100
            : 0;

        var completedWithDuration = executions
            .Where(e => e.DurationMs.HasValue && e.DurationMs.Value > 0)
            .ToList();

        var averageDurationMs = completedWithDuration.Any()
            ? (long)completedWithDuration.Average(e => e.DurationMs!.Value)
            : (long?)null;

        var executionsByType = executions
            .GroupBy(e => e.ExecutionType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var executionsByState = executions
            .GroupBy(e => e.State.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var statistics = new ExecutionStatistics
        {
            TotalExecutions = totalExecutions,
            CompletedExecutions = completedExecutions,
            FailedExecutions = failedExecutions,
            CancelledExecutions = cancelledExecutions,
            RunningExecutions = runningExecutions,
            QueuedExecutions = queuedExecutions,
            SuccessRate = successRate,
            AverageDurationMs = averageDurationMs,
            ExecutionsByType = executionsByType,
            ExecutionsByState = executionsByState,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo
        };

        _logger.LogDebug(
            "Calculated execution statistics: Total={Total}, Success={Success}, Failed={Failed}, SuccessRate={SuccessRate:F2}%",
            totalExecutions, completedExecutions, failedExecutions, successRate);

        return statistics;
    }

    public async Task<int> DeleteOldExecutionsAsync(DateTime olderThan, string userId)
    {
        var oldExecutions = await _dbContext.ExecutionHistory
            .Where(e => e.StartedAt < olderThan)
            .ToListAsync();

        var count = oldExecutions.Count;

        if (count > 0)
        {
            _dbContext.ExecutionHistory.RemoveRange(oldExecutions);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Deleted {Count} execution history records older than {Date} (User: {UserId})",
                count, olderThan, userId);
        }

        return count;
    }

    public async Task DeleteExecutionAsync(Guid executionId, string userId)
    {
        var execution = await _dbContext.ExecutionHistory
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId);

        if (execution == null)
        {
            throw new KeyNotFoundException($"Execution with ID {executionId} not found");
        }

        _dbContext.ExecutionHistory.Remove(execution);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted execution history record {ExecutionId} (User: {UserId})",
            executionId, userId);
    }

    private IQueryable<ExecutionHistoryEntity> ApplySorting(
        IQueryable<ExecutionHistoryEntity> queryable,
        string sortBy,
        string sortDirection)
    {
        var isDescending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLower() switch
        {
            "startedat" => isDescending
                ? queryable.OrderByDescending(e => e.StartedAt)
                : queryable.OrderBy(e => e.StartedAt),
            "completedat" => isDescending
                ? queryable.OrderByDescending(e => e.CompletedAt)
                : queryable.OrderBy(e => e.CompletedAt),
            "executionname" => isDescending
                ? queryable.OrderByDescending(e => e.ExecutionName)
                : queryable.OrderBy(e => e.ExecutionName),
            "state" => isDescending
                ? queryable.OrderByDescending(e => e.State)
                : queryable.OrderBy(e => e.State),
            "durationms" => isDescending
                ? queryable.OrderByDescending(e => e.DurationMs)
                : queryable.OrderBy(e => e.DurationMs),
            _ => queryable.OrderByDescending(e => e.StartedAt) // Default: newest first
        };
    }
}
