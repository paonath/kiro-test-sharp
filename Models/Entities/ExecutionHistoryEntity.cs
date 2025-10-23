namespace BoltWebAPI.Models.Entities;

public enum ExecutionType
{
    Command,
    Task,
    Plan
}

public enum ExecutionState
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class ExecutionHistoryEntity
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public ExecutionType Type { get; set; }
    public ExecutionType ExecutionType { get; set; }
    public string Command { get; set; } = string.Empty;
    public string ExecutionName { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string? Targets { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? ExecutionTime { get; set; }
    public long? DurationMs { get; set; }
    public int? ExitCode { get; set; }
    public string? StandardOutput { get; set; }
    public string? StandardError { get; set; }
    public bool Success { get; set; }
    public ExecutionState State { get; set; }
    public string? ErrorMessage { get; set; }
}
