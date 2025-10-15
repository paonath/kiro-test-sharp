using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using BoltWebAPI.Services.Interfaces;

namespace BoltWebAPI.Services.Implementations;

/// <summary>
/// Manages execution of external processes with real-time output capture and process tracking
/// </summary>
public class ProcessManager : IProcessManager
{
    private readonly ILogger<ProcessManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<Guid, ProcessTracker> _runningProcesses;
    private readonly int _defaultTimeoutSeconds;
    private readonly int _maxConcurrentExecutions;
    private readonly SemaphoreSlim _concurrencyLimiter;

    public ProcessManager(ILogger<ProcessManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _runningProcesses = new ConcurrentDictionary<Guid, ProcessTracker>();
        
        _defaultTimeoutSeconds = configuration.GetValue<int>("Bolt:DefaultTimeout", 300);
        _maxConcurrentExecutions = configuration.GetValue<int>("Bolt:MaxConcurrentExecutions", 5);
        _concurrencyLimiter = new SemaphoreSlim(_maxConcurrentExecutions, _maxConcurrentExecutions);
    }

    public async Task<ProcessExecutionResult> ExecuteAsync(
        string executable,
        string[] arguments,
        Action<string>? onStdOut = null,
        Action<string>? onStdErr = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var stdOutBuilder = new StringBuilder();
        var stdErrBuilder = new StringBuilder();
        var timedOut = false;
        var cancelled = false;
        
        // Use provided timeout or default
        var effectiveTimeout = timeoutSeconds ?? _defaultTimeoutSeconds;

        _logger.LogInformation(
            "Starting process execution {ExecutionId}: {Executable} {Arguments} (Timeout: {Timeout}s)",
            executionId,
            executable,
            string.Join(" ", arguments),
            effectiveTimeout);

        // Wait for available execution slot
        await _concurrencyLimiter.WaitAsync(cancellationToken);

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _configuration["Bolt:WorkingDirectory"] ?? Environment.CurrentDirectory
            };

            // Add arguments
            foreach (var arg in arguments)
            {
                processStartInfo.ArgumentList.Add(arg);
            }

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            var processTracker = new ProcessTracker
            {
                ExecutionId = executionId,
                Process = process,
                StartTime = startTime
            };

            // Track the process
            _runningProcesses.TryAdd(executionId, processTracker);

            // Set up output handlers
            var outputCompletionSource = new TaskCompletionSource<bool>();
            var errorCompletionSource = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    stdOutBuilder.AppendLine(e.Data);
                    onStdOut?.Invoke(e.Data);
                }
                else
                {
                    outputCompletionSource.TrySetResult(true);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    stdErrBuilder.AppendLine(e.Data);
                    onStdErr?.Invoke(e.Data);
                }
                else
                {
                    errorCompletionSource.TrySetResult(true);
                }
            };

            // Start the process
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start process");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _logger.LogDebug(
                "Process {ExecutionId} started with PID {ProcessId}",
                executionId,
                process.Id);

            // Create timeout cancellation token
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(effectiveTimeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                // Wait for process to exit
                await process.WaitForExitAsync(linkedCts.Token);

                // Wait for output streams to complete
                await Task.WhenAll(
                    outputCompletionSource.Task,
                    errorCompletionSource.Task);
            }
            catch (OperationCanceledException)
            {
                if (timeoutCts.Token.IsCancellationRequested)
                {
                    timedOut = true;
                    _logger.LogWarning(
                        "Process {ExecutionId} timed out after {Timeout} seconds",
                        executionId,
                        effectiveTimeout);
                }
                else
                {
                    cancelled = true;
                    _logger.LogInformation(
                        "Process {ExecutionId} was cancelled",
                        executionId);
                }

                // Kill the process
                await KillProcessInternalAsync(process, executionId);
            }

            var completedAt = DateTime.UtcNow;
            var executionTime = completedAt - startTime;

            var result = new ProcessExecutionResult
            {
                ExecutionId = executionId,
                ExitCode = timedOut || cancelled ? -1 : process.ExitCode,
                StandardOutput = stdOutBuilder.ToString(),
                StandardError = stdErrBuilder.ToString(),
                ExecutionTime = executionTime,
                StartedAt = startTime,
                CompletedAt = completedAt,
                TimedOut = timedOut,
                Cancelled = cancelled
            };

            _logger.LogInformation(
                "Process {ExecutionId} completed with exit code {ExitCode} in {ExecutionTime}ms",
                executionId,
                result.ExitCode,
                executionTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing process {ExecutionId}: {Executable}",
                executionId,
                executable);
            throw;
        }
        finally
        {
            // Clean up process tracking
            _runningProcesses.TryRemove(executionId, out _);
            
            // Release concurrency slot
            _concurrencyLimiter.Release();
        }
    }

    public Task<bool> IsProcessRunningAsync(Guid executionId)
    {
        if (_runningProcesses.TryGetValue(executionId, out var tracker))
        {
            try
            {
                return Task.FromResult(!tracker.Process.HasExited);
            }
            catch
            {
                // Process may have been disposed
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(false);
    }

    public async Task KillProcessAsync(Guid executionId)
    {
        if (_runningProcesses.TryGetValue(executionId, out var tracker))
        {
            _logger.LogInformation("Killing process {ExecutionId}", executionId);
            await KillProcessInternalAsync(tracker.Process, executionId);
        }
        else
        {
            _logger.LogWarning("Process {ExecutionId} not found or already completed", executionId);
        }
    }

    private async Task KillProcessInternalAsync(Process process, Guid executionId)
    {
        try
        {
            if (!process.HasExited)
            {
                // Try graceful termination first
                process.Kill(entireProcessTree: true);
                
                // Wait a bit for the process to exit
                await Task.Delay(1000);
                
                // Force kill if still running
                if (!process.HasExited)
                {
                    process.Kill();
                }
                
                _logger.LogInformation("Process {ExecutionId} killed successfully", executionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing process {ExecutionId}", executionId);
        }
    }

    /// <summary>
    /// Internal class to track running processes
    /// </summary>
    private class ProcessTracker
    {
        public Guid ExecutionId { get; set; }
        public Process Process { get; set; } = null!;
        public DateTime StartTime { get; set; }
    }
}
