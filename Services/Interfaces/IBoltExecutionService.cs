using BoltWebAPI.Models.Domain;

namespace BoltWebAPI.Services.Interfaces;

/// <summary>
/// Service for executing Puppet Bolt commands, tasks, and plans
/// </summary>
public interface IBoltExecutionService
{
    // ========================================================================
    // COMMAND EXECUTION
    // ========================================================================

    /// <summary>
    /// Executes an arbitrary Bolt command with specified arguments
    /// </summary>
    /// <param name="command">The Bolt command to execute (e.g., "inventory", "command run")</param>
    /// <param name="arguments">Command arguments</param>
    /// <param name="userId">ID of the user executing the command</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds (overrides default from configuration)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result of the command execution including output and exit code</returns>
    /// <exception cref="ArgumentException">Thrown when command or userId is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when Bolt CLI is not available or execution fails</exception>
    /// <exception cref="TimeoutException">Thrown when execution exceeds timeout limit</exception>
    Task<CommandExecutionResult> ExecuteCommandAsync(
        string command,
        string[] arguments,
        string userId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // TASK EXECUTION
    // ========================================================================

    /// <summary>
    /// Executes a Bolt task on specified target nodes
    /// </summary>
    /// <param name="taskName">Fully qualified task name (e.g., "package::install")</param>
    /// <param name="targets">Target nodes for task execution</param>
    /// <param name="parameters">Task parameters as key-value pairs</param>
    /// <param name="userId">ID of the user executing the task</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result of the task execution including per-node results</returns>
    /// <exception cref="ArgumentException">Thrown when taskName, targets, or userId is invalid</exception>
    /// <exception cref="KeyNotFoundException">Thrown when task does not exist</exception>
    /// <exception cref="InvalidOperationException">Thrown when task execution fails</exception>
    Task<TaskExecutionResult> ExecuteTaskAsync(
        string taskName,
        string[] targets,
        Dictionary<string, object> parameters,
        string userId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // PLAN EXECUTION
    // ========================================================================

    /// <summary>
    /// Executes a Bolt plan with specified parameters
    /// </summary>
    /// <param name="planName">Fully qualified plan name (e.g., "mymodule::deploy")</param>
    /// <param name="parameters">Plan parameters as key-value pairs</param>
    /// <param name="userId">ID of the user executing the plan</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result of the plan execution including return value and output</returns>
    /// <exception cref="ArgumentException">Thrown when planName or userId is invalid</exception>
    /// <exception cref="KeyNotFoundException">Thrown when plan does not exist</exception>
    /// <exception cref="InvalidOperationException">Thrown when plan execution fails</exception>
    Task<PlanExecutionResult> ExecutePlanAsync(
        string planName,
        Dictionary<string, object> parameters,
        string userId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // EXECUTION MANAGEMENT
    // ========================================================================

    /// <summary>
    /// Gets the current status of an ongoing or completed execution
    /// </summary>
    /// <param name="executionId">Unique identifier of the execution</param>
    /// <returns>Current execution status including state and accumulated output</returns>
    /// <exception cref="KeyNotFoundException">Thrown when execution ID is not found</exception>
    Task<ExecutionStatus> GetExecutionStatusAsync(Guid executionId);

    /// <summary>
    /// Cancels a running execution
    /// </summary>
    /// <param name="executionId">Unique identifier of the execution to cancel</param>
    /// <returns>Task representing the cancellation operation</returns>
    /// <exception cref="KeyNotFoundException">Thrown when execution ID is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when execution cannot be cancelled (already completed)</exception>
    Task CancelExecutionAsync(Guid executionId);

    // ========================================================================
    // TASK DISCOVERY
    // ========================================================================

    /// <summary>
    /// Lists all available Bolt tasks from configured modules
    /// </summary>
    /// <returns>Collection of available tasks with basic information</returns>
    /// <exception cref="InvalidOperationException">Thrown when Bolt CLI is not available or task listing fails</exception>
    Task<IEnumerable<BoltTask>> ListTasksAsync();

    /// <summary>
    /// Gets detailed information about a specific Bolt task including parameters
    /// </summary>
    /// <param name="taskName">Fully qualified task name</param>
    /// <returns>Detailed task information including parameter specifications</returns>
    /// <exception cref="ArgumentException">Thrown when taskName is null or empty</exception>
    /// <exception cref="KeyNotFoundException">Thrown when task does not exist</exception>
    Task<BoltTaskDetails> GetTaskDetailsAsync(string taskName);

    // ========================================================================
    // PLAN DISCOVERY
    // ========================================================================

    /// <summary>
    /// Lists all available Bolt plans from configured modules
    /// </summary>
    /// <returns>Collection of available plans with basic information</returns>
    /// <exception cref="InvalidOperationException">Thrown when Bolt CLI is not available or plan listing fails</exception>
    Task<IEnumerable<BoltPlan>> ListPlansAsync();

    /// <summary>
    /// Gets detailed information about a specific Bolt plan including parameters
    /// </summary>
    /// <param name="planName">Fully qualified plan name</param>
    /// <returns>Detailed plan information including parameter specifications</returns>
    /// <exception cref="ArgumentException">Thrown when planName is null or empty</exception>
    /// <exception cref="KeyNotFoundException">Thrown when plan does not exist</exception>
    Task<BoltPlanDetails> GetPlanDetailsAsync(string planName);
}
