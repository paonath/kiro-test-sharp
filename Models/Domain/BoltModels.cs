namespace BoltWebAPI.Models.Domain;

// ============================================================================
// REQUEST MODELS
// ============================================================================

/// <summary>
/// Request model for executing a Bolt command
/// </summary>
public class CommandExecutionRequest
{
    /// <summary>
    /// The Bolt command to execute (e.g., "inventory", "task run", etc.)
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Arguments to pass to the command
    /// </summary>
    public string[] Arguments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Optional timeout in seconds (overrides default from configuration)
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Request model for executing a Bolt task
/// </summary>
public class TaskExecutionRequest
{
    /// <summary>
    /// Name of the task to execute (e.g., "package::install")
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// Target nodes for task execution
    /// </summary>
    public string[] Targets { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Parameters to pass to the task
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Optional timeout in seconds
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Request model for executing a Bolt plan
/// </summary>
public class PlanExecutionRequest
{
    /// <summary>
    /// Name of the plan to execute (e.g., "mymodule::deploy")
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Parameters to pass to the plan
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Optional timeout in seconds
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

// ============================================================================
// RESPONSE MODELS
// ============================================================================

/// <summary>
/// Result of a Bolt command execution
/// </summary>
public class CommandExecutionResult
{
    /// <summary>
    /// Unique identifier for this execution
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Exit code returned by the Bolt CLI process
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Standard output from the command
    /// </summary>
    public string StandardOutput { get; set; } = string.Empty;

    /// <summary>
    /// Standard error output from the command
    /// </summary>
    public string StandardError { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to execute the command
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// When the execution started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the execution completed
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Whether the execution was successful (exit code 0)
    /// </summary>
    public bool Success { get; set; }
}

/// <summary>
/// Result of a Bolt task execution
/// </summary>
public class TaskExecutionResult
{
    /// <summary>
    /// Unique identifier for this execution
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Name of the task that was executed
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// Results for each target node
    /// </summary>
    public List<NodeResult> NodeResults { get; set; } = new();

    /// <summary>
    /// Total time taken to execute the task across all nodes
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Whether all node executions were successful
    /// </summary>
    public bool Success { get; set; }
}

/// <summary>
/// Result of task execution on a single node
/// </summary>
public class NodeResult
{
    /// <summary>
    /// Name or identifier of the node
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the task succeeded on this node
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result data returned by the task (JSON deserialized)
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Error message if the task failed on this node
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Result of a Bolt plan execution
/// </summary>
public class PlanExecutionResult
{
    /// <summary>
    /// Unique identifier for this execution
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Name of the plan that was executed
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Return value from the plan (if any)
    /// </summary>
    public object? ReturnValue { get; set; }

    /// <summary>
    /// Complete output from the plan execution
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to execute the plan
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Whether the plan executed successfully
    /// </summary>
    public bool Success { get; set; }
}

/// <summary>
/// Current status of an ongoing or completed execution
/// </summary>
public class ExecutionStatus
{
    /// <summary>
    /// Unique identifier for the execution
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Current state of the execution
    /// </summary>
    public ExecutionState State { get; set; }

    /// <summary>
    /// Current accumulated output
    /// </summary>
    public string CurrentOutput { get; set; } = string.Empty;

    /// <summary>
    /// When the execution started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Time elapsed since execution started
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }
}

/// <summary>
/// Execution state enumeration (matches ExecutionHistoryEntity.ExecutionState)
/// </summary>
public enum ExecutionState
{
    /// <summary>
    /// Execution is queued and waiting to start
    /// </summary>
    Queued,

    /// <summary>
    /// Execution is currently running
    /// </summary>
    Running,

    /// <summary>
    /// Execution completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Execution failed with errors
    /// </summary>
    Failed,

    /// <summary>
    /// Execution was cancelled by user
    /// </summary>
    Cancelled
}

// ============================================================================
// TASK MODELS
// ============================================================================

/// <summary>
/// Basic information about a Bolt task
/// </summary>
public class BoltTask
{
    /// <summary>
    /// Fully qualified task name (e.g., "package::install")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Task description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Module that provides this task
    /// </summary>
    public string Module { get; set; } = string.Empty;
}

/// <summary>
/// Detailed information about a Bolt task including parameters
/// </summary>
public class BoltTaskDetails : BoltTask
{
    /// <summary>
    /// Task parameters with their specifications
    /// </summary>
    public Dictionary<string, TaskParameter> Parameters { get; set; } = new();
}

/// <summary>
/// Specification for a task parameter
/// </summary>
public class TaskParameter
{
    /// <summary>
    /// Data type of the parameter (e.g., "String", "Integer", "Boolean")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Parameter description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this parameter is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Default value for the parameter (if any)
    /// </summary>
    public object? Default { get; set; }
}

// ============================================================================
// PLAN MODELS
// ============================================================================

/// <summary>
/// Basic information about a Bolt plan
/// </summary>
public class BoltPlan
{
    /// <summary>
    /// Fully qualified plan name (e.g., "mymodule::deploy")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Plan description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Module that provides this plan
    /// </summary>
    public string Module { get; set; } = string.Empty;
}

/// <summary>
/// Detailed information about a Bolt plan including parameters
/// </summary>
public class BoltPlanDetails : BoltPlan
{
    /// <summary>
    /// Plan parameters with their specifications
    /// </summary>
    public Dictionary<string, PlanParameter> Parameters { get; set; } = new();
}

/// <summary>
/// Specification for a plan parameter
/// </summary>
public class PlanParameter
{
    /// <summary>
    /// Data type of the parameter (e.g., "String", "TargetSpec", "Hash")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Parameter description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this parameter is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Default value for the parameter (if any)
    /// </summary>
    public object? Default { get; set; }
}
