namespace BoltWebAPI.Models.Domain;

/// <summary>
/// Execution history record for display
/// </summary>
public class ExecutionHistoryRecord
{
    /// <summary>
    /// Execution unique identifier
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Type of execution (Command, Task, Plan)
    /// </summary>
    public string ExecutionType { get; set; } = string.Empty;

    /// <summary>
    /// Command, task name, or plan name
    /// </summary>
    public string ExecutionName { get; set; } = string.Empty;

    /// <summary>
    /// Target nodes (for tasks)
    /// </summary>
    public string Targets { get; set; } = string.Empty;

    /// <summary>
    /// Execution state (Queued, Running, Completed, Failed, Cancelled)
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Exit code (for commands)
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// Standard output
    /// </summary>
    public string StandardOutput { get; set; } = string.Empty;

    /// <summary>
    /// Standard error
    /// </summary>
    public string StandardError { get; set; } = string.Empty;

    /// <summary>
    /// User who initiated the execution
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username who initiated the execution
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// When execution started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When execution completed (null if still running)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Duration in milliseconds (null if still running)
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Paginated execution history response
/// </summary>
public class ExecutionHistoryResponse
{
    /// <summary>
    /// List of execution history records
    /// </summary>
    public List<ExecutionHistoryRecord> Records { get; set; } = new();

    /// <summary>
    /// Total number of records matching filter
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of records per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Request to query execution history with filters
/// </summary>
public class ExecutionHistoryQuery
{
    /// <summary>
    /// Filter by execution type (Command, Task, Plan)
    /// </summary>
    public string? ExecutionType { get; set; }

    /// <summary>
    /// Filter by execution state (Queued, Running, Completed, Failed, Cancelled)
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Filter by user ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by execution name (command, task name, or plan name) - partial match
    /// </summary>
    public string? ExecutionName { get; set; }

    /// <summary>
    /// Filter by start date (from)
    /// </summary>
    public DateTime? StartDateFrom { get; set; }

    /// <summary>
    /// Filter by start date (to)
    /// </summary>
    public DateTime? StartDateTo { get; set; }

    /// <summary>
    /// Page number (1-based, default: 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size (default: 20, max: 100)
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field (StartedAt, CompletedAt, ExecutionName, State)
    /// </summary>
    public string SortBy { get; set; } = "StartedAt";

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Execution statistics summary
/// </summary>
public class ExecutionStatistics
{
    /// <summary>
    /// Total number of executions
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Number of completed executions
    /// </summary>
    public int CompletedExecutions { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Number of cancelled executions
    /// </summary>
    public int CancelledExecutions { get; set; }

    /// <summary>
    /// Number of currently running executions
    /// </summary>
    public int RunningExecutions { get; set; }

    /// <summary>
    /// Number of queued executions
    /// </summary>
    public int QueuedExecutions { get; set; }

    /// <summary>
    /// Success rate percentage (0-100)
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average execution duration in milliseconds
    /// </summary>
    public long? AverageDurationMs { get; set; }

    /// <summary>
    /// Total executions by type
    /// </summary>
    public Dictionary<string, int> ExecutionsByType { get; set; } = new();

    /// <summary>
    /// Total executions by state
    /// </summary>
    public Dictionary<string, int> ExecutionsByState { get; set; } = new();

    /// <summary>
    /// Period covered by statistics
    /// </summary>
    public DateTime? PeriodFrom { get; set; }

    /// <summary>
    /// Period covered by statistics
    /// </summary>
    public DateTime? PeriodTo { get; set; }
}
