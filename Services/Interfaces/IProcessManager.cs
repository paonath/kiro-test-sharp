namespace BoltWebAPI.Services.Interfaces;

/// <summary>
/// Manages execution of external processes (Bolt CLI) with real-time output capture
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// Executes an external process asynchronously with real-time output callbacks
    /// </summary>
    /// <param name="executable">Path to the executable</param>
    /// <param name="arguments">Command-line arguments</param>
    /// <param name="onStdOut">Callback for stdout output (optional)</param>
    /// <param name="onStdErr">Callback for stderr output (optional)</param>
    /// <param name="timeoutSeconds">Timeout in seconds (optional, uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process execution result</returns>
    Task<ProcessExecutionResult> ExecuteAsync(
        string executable,
        string[] arguments,
        Action<string>? onStdOut = null,
        Action<string>? onStdErr = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a process is currently running
    /// </summary>
    /// <param name="executionId">Execution ID</param>
    /// <returns>True if process is running, false otherwise</returns>
    Task<bool> IsProcessRunningAsync(Guid executionId);

    /// <summary>
    /// Terminates a running process
    /// </summary>
    /// <param name="executionId">Execution ID</param>
    Task KillProcessAsync(Guid executionId);
}

/// <summary>
/// Result of a process execution
/// </summary>
public class ProcessExecutionResult
{
    public Guid ExecutionId { get; set; }
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public bool Success => ExitCode == 0;
    public bool TimedOut { get; set; }
    public bool Cancelled { get; set; }
}
