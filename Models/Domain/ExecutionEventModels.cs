namespace BoltWebAPI.Models.Domain;

/// <summary>
/// Real-time execution event sent via SignalR
/// </summary>
public class ExecutionEvent
{
    /// <summary>
    /// Execution unique identifier
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Event type (Started, OutputReceived, StateChanged, Completed, Failed, Cancelled)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Execution type (Command, Task, Plan)
    /// </summary>
    public string ExecutionType { get; set; } = string.Empty;

    /// <summary>
    /// Execution name (command, task name, or plan name)
    /// </summary>
    public string ExecutionName { get; set; } = string.Empty;

    /// <summary>
    /// Current execution state
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// User who initiated the execution
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username who initiated the execution
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Event data (varies by event type)
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Execution started event
/// </summary>
public class ExecutionStartedEvent : ExecutionEvent
{
    public ExecutionStartedEvent()
    {
        EventType = "Started";
    }

    /// <summary>
    /// When execution started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Target nodes (for tasks)
    /// </summary>
    public string? Targets { get; set; }

    /// <summary>
    /// Execution parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Execution output received event (real-time stdout/stderr)
/// </summary>
public class ExecutionOutputEvent : ExecutionEvent
{
    public ExecutionOutputEvent()
    {
        EventType = "OutputReceived";
    }

    /// <summary>
    /// Output type (stdout, stderr)
    /// </summary>
    public string OutputType { get; set; } = string.Empty;

    /// <summary>
    /// Output content
    /// </summary>
    public string Output { get; set; } = string.Empty;
}

/// <summary>
/// Execution state changed event
/// </summary>
public class ExecutionStateChangedEvent : ExecutionEvent
{
    public ExecutionStateChangedEvent()
    {
        EventType = "StateChanged";
    }

    /// <summary>
    /// Previous state
    /// </summary>
    public string PreviousState { get; set; } = string.Empty;

    /// <summary>
    /// New state
    /// </summary>
    public string NewState { get; set; } = string.Empty;
}

/// <summary>
/// Execution completed event
/// </summary>
public class ExecutionCompletedEvent : ExecutionEvent
{
    public ExecutionCompletedEvent()
    {
        EventType = "Completed";
        State = "Completed";
    }

    /// <summary>
    /// When execution completed
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Exit code (for commands)
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Final output summary
    /// </summary>
    public string? Summary { get; set; }
}

/// <summary>
/// Execution failed event
/// </summary>
public class ExecutionFailedEvent : ExecutionEvent
{
    public ExecutionFailedEvent()
    {
        EventType = "Failed";
        State = "Failed";
    }

    /// <summary>
    /// When execution failed
    /// </summary>
    public DateTime FailedAt { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Exit code (if available)
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// Error details
    /// </summary>
    public string? ErrorDetails { get; set; }
}

/// <summary>
/// Execution cancelled event
/// </summary>
public class ExecutionCancelledEvent : ExecutionEvent
{
    public ExecutionCancelledEvent()
    {
        EventType = "Cancelled";
        State = "Cancelled";
    }

    /// <summary>
    /// When execution was cancelled
    /// </summary>
    public DateTime CancelledAt { get; set; }

    /// <summary>
    /// User who cancelled the execution
    /// </summary>
    public string? CancelledByUserId { get; set; }

    /// <summary>
    /// Username who cancelled the execution
    /// </summary>
    public string? CancelledByUsername { get; set; }
}
