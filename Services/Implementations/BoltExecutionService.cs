using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using BoltWebAPI.Data;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Models.Entities;
using BoltWebAPI.Services.Interfaces;
using BoltWebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BoltWebAPI.Services.Implementations;

/// <summary>
/// Implementation of Bolt execution service for managing Bolt CLI operations
/// </summary>
public class BoltExecutionService : IBoltExecutionService
{
    private readonly IProcessManager _processManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BoltExecutionService> _logger;
    private readonly BoltDbContext _dbContext;
    private readonly IHubContext<ExecutionHub> _hubContext;
    private readonly ConcurrentDictionary<Guid, ExecutionTracker> _activeExecutions;
    private readonly string _boltExecutablePath;
    private readonly string _workingDirectory;

    public BoltExecutionService(
        IProcessManager processManager,
        IConfiguration configuration,
        ILogger<BoltExecutionService> logger,
        BoltDbContext dbContext,
        IHubContext<ExecutionHub> hubContext)
    {
        _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _activeExecutions = new ConcurrentDictionary<Guid, ExecutionTracker>();

        _boltExecutablePath = _configuration["Bolt:ExecutablePath"] ?? "bolt";
        _workingDirectory = _configuration["Bolt:WorkingDirectory"] ?? Directory.GetCurrentDirectory();
    }

    // ========================================================================
    // COMMAND EXECUTION
    // ========================================================================

    public async Task<CommandExecutionResult> ExecuteCommandAsync(
        string command,
        string[] arguments,
        string userId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        arguments ??= Array.Empty<string>();

        var executionId = Guid.NewGuid();
        var tracker = new ExecutionTracker
        {
            ExecutionId = executionId,
            State = Models.Domain.ExecutionState.Running,
            Output = new StringBuilder(),
            ErrorOutput = new StringBuilder(),
            StartTime = DateTime.UtcNow
        };

        _activeExecutions.TryAdd(executionId, tracker);

        _logger.LogInformation(
            "Starting command execution: {Command} {Arguments} (ExecutionId: {ExecutionId}, UserId: {UserId})",
            command, string.Join(" ", arguments), executionId, userId);

        try
        {
            // Build Bolt CLI arguments
            var boltArgs = new List<string> { command };
            boltArgs.AddRange(arguments);
            boltArgs.Add("--format");
            boltArgs.Add("json");

            // Execute process
            var processResult = await _processManager.ExecuteAsync(
                _boltExecutablePath,
                boltArgs.ToArray(),
                onStdOut: output => tracker.Output.AppendLine(output),
                onStdErr: error => tracker.ErrorOutput.AppendLine(error),
                timeoutSeconds,
                cancellationToken);

            // Update tracker state
            tracker.State = processResult.Success ? Models.Domain.ExecutionState.Completed : Models.Domain.ExecutionState.Failed;

            var result = new CommandExecutionResult
            {
                ExecutionId = executionId,
                ExitCode = processResult.ExitCode,
                StandardOutput = tracker.Output.ToString(),
                StandardError = tracker.ErrorOutput.ToString(),
                ExecutionTime = processResult.ExecutionTime,
                StartedAt = processResult.StartedAt,
                CompletedAt = processResult.CompletedAt,
                Success = processResult.Success
            };

            // Log to database
            await LogExecutionToDatabase(
                executionId,
                userId,
                ExecutionType.Command,
                command,
                string.Join(" ", arguments),
                result.StartedAt,
                result.CompletedAt,
                result.ExecutionTime,
                result.ExitCode,
                result.StandardOutput,
                result.StandardError,
                result.Success,
                ConvertToEntityState(tracker.State));

            _logger.LogInformation(
                "Command execution completed: {ExecutionId}, ExitCode: {ExitCode}, Success: {Success}",
                executionId, result.ExitCode, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            tracker.State = Models.Domain.ExecutionState.Failed;
            _logger.LogError(ex, "Command execution failed: {ExecutionId}", executionId);
            throw;
        }
    }

    // ========================================================================
    // TASK EXECUTION
    // ========================================================================

    public async Task<TaskExecutionResult> ExecuteTaskAsync(
        string taskName,
        string[] targets,
        Dictionary<string, object> parameters,
        string userId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(taskName))
            throw new ArgumentException("Task name cannot be null or empty", nameof(taskName));
        if (targets == null || targets.Length == 0)
            throw new ArgumentException("At least one target must be specified", nameof(targets));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        parameters ??= new Dictionary<string, object>();

        var executionId = Guid.NewGuid();
        var tracker = new ExecutionTracker
        {
            ExecutionId = executionId,
            State = Models.Domain.ExecutionState.Running,
            Output = new StringBuilder(),
            ErrorOutput = new StringBuilder(),
            StartTime = DateTime.UtcNow
        };

        _activeExecutions.TryAdd(executionId, tracker);

        _logger.LogInformation(
            "Starting task execution: {TaskName} on {TargetCount} targets (ExecutionId: {ExecutionId}, UserId: {UserId})",
            taskName, targets.Length, executionId, userId);

        try
        {
            // Build Bolt CLI arguments
            var boltArgs = new List<string> { "task", "run", taskName };
            boltArgs.Add("--targets");
            boltArgs.Add(string.Join(",", targets));

            if (parameters.Count > 0)
            {
                boltArgs.Add("--params");
                boltArgs.Add(JsonSerializer.Serialize(parameters));
            }

            boltArgs.Add("--format");
            boltArgs.Add("json");

            // Execute process
            var processResult = await _processManager.ExecuteAsync(
                _boltExecutablePath,
                boltArgs.ToArray(),
                onStdOut: output => tracker.Output.AppendLine(output),
                onStdErr: error => tracker.ErrorOutput.AppendLine(error),
                timeoutSeconds,
                cancellationToken);

            tracker.State = processResult.Success ? Models.Domain.ExecutionState.Completed : Models.Domain.ExecutionState.Failed;

            // Parse task results from JSON output
            var nodeResults = ParseTaskResults(tracker.Output.ToString());

            var result = new TaskExecutionResult
            {
                ExecutionId = executionId,
                TaskName = taskName,
                NodeResults = nodeResults,
                ExecutionTime = processResult.ExecutionTime,
                Success = processResult.Success && nodeResults.All(n => n.Success)
            };

            // Log to database
            await LogExecutionToDatabase(
                executionId,
                userId,
                ExecutionType.Task,
                taskName,
                JsonSerializer.Serialize(new { targets, parameters }),
                processResult.StartedAt,
                processResult.CompletedAt,
                processResult.ExecutionTime,
                processResult.ExitCode,
                tracker.Output.ToString(),
                tracker.ErrorOutput.ToString(),
                result.Success,
                ConvertToEntityState(tracker.State));

            _logger.LogInformation(
                "Task execution completed: {ExecutionId}, Success: {Success}, Nodes: {SuccessCount}/{TotalCount}",
                executionId, result.Success, nodeResults.Count(n => n.Success), nodeResults.Count);

            return result;
        }
        catch (Exception ex)
        {
            tracker.State = Models.Domain.ExecutionState.Failed;
            _logger.LogError(ex, "Task execution failed: {ExecutionId}, Task: {TaskName}", executionId, taskName);
            throw;
        }
    }

    // ========================================================================
    // PLAN EXECUTION
    // ========================================================================

    public async Task<PlanExecutionResult> ExecutePlanAsync(
        string planName,
        Dictionary<string, object> parameters,
        string userId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(planName))
            throw new ArgumentException("Plan name cannot be null or empty", nameof(planName));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        parameters ??= new Dictionary<string, object>();

        var executionId = Guid.NewGuid();
        var tracker = new ExecutionTracker
        {
            ExecutionId = executionId,
            State = Models.Domain.ExecutionState.Running,
            Output = new StringBuilder(),
            ErrorOutput = new StringBuilder(),
            StartTime = DateTime.UtcNow
        };

        _activeExecutions.TryAdd(executionId, tracker);

        _logger.LogInformation(
            "Starting plan execution: {PlanName} (ExecutionId: {ExecutionId}, UserId: {UserId})",
            planName, executionId, userId);

        try
        {
            // Build Bolt CLI arguments
            var boltArgs = new List<string> { "plan", "run", planName };

            if (parameters.Count > 0)
            {
                boltArgs.Add("--params");
                boltArgs.Add(JsonSerializer.Serialize(parameters));
            }

            boltArgs.Add("--format");
            boltArgs.Add("json");

            // Execute process
            var processResult = await _processManager.ExecuteAsync(
                _boltExecutablePath,
                boltArgs.ToArray(),
                onStdOut: output => tracker.Output.AppendLine(output),
                onStdErr: error => tracker.ErrorOutput.AppendLine(error),
                timeoutSeconds,
                cancellationToken);

            tracker.State = processResult.Success ? Models.Domain.ExecutionState.Completed : Models.Domain.ExecutionState.Failed;

            // Parse plan results from JSON output
            var returnValue = ParsePlanReturnValue(tracker.Output.ToString());

            var result = new PlanExecutionResult
            {
                ExecutionId = executionId,
                PlanName = planName,
                ReturnValue = returnValue,
                Output = tracker.Output.ToString(),
                ExecutionTime = processResult.ExecutionTime,
                Success = processResult.Success
            };

            // Log to database
            await LogExecutionToDatabase(
                executionId,
                userId,
                ExecutionType.Plan,
                planName,
                JsonSerializer.Serialize(parameters),
                processResult.StartedAt,
                processResult.CompletedAt,
                processResult.ExecutionTime,
                processResult.ExitCode,
                tracker.Output.ToString(),
                tracker.ErrorOutput.ToString(),
                result.Success,
                ConvertToEntityState(tracker.State));

            _logger.LogInformation(
                "Plan execution completed: {ExecutionId}, Success: {Success}",
                executionId, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            tracker.State = Models.Domain.ExecutionState.Failed;
            _logger.LogError(ex, "Plan execution failed: {ExecutionId}, Plan: {PlanName}", executionId, planName);
            throw;
        }
    }

    // ========================================================================
    // EXECUTION MANAGEMENT
    // ========================================================================

    public async Task<ExecutionStatus> GetExecutionStatusAsync(Guid executionId)
    {
        // Check active executions first
        if (_activeExecutions.TryGetValue(executionId, out var tracker))
        {
            return new ExecutionStatus
            {
                ExecutionId = executionId,
                State = tracker.State,
                CurrentOutput = tracker.Output.ToString(),
                StartedAt = tracker.StartTime,
                ElapsedTime = DateTime.UtcNow - tracker.StartTime
            };
        }

        // Check database for historical executions
        var historyEntry = await _dbContext.ExecutionHistory
            .Where(e => e.Id == executionId)
            .FirstOrDefaultAsync();

        if (historyEntry != null)
        {
            return new ExecutionStatus
            {
                ExecutionId = executionId,
                State = ConvertToDomainState(historyEntry.State),
                CurrentOutput = historyEntry.StandardOutput ?? string.Empty,
                StartedAt = historyEntry.StartedAt,
                ElapsedTime = historyEntry.ExecutionTime ?? TimeSpan.Zero
            };
        }

        throw new KeyNotFoundException($"Execution with ID {executionId} not found");
    }

    public async Task CancelExecutionAsync(Guid executionId)
    {
        if (!_activeExecutions.TryGetValue(executionId, out var tracker))
        {
            throw new KeyNotFoundException($"Execution with ID {executionId} not found or already completed");
        }

        _logger.LogInformation("Cancelling execution: {ExecutionId}", executionId);

        try
        {
            await _processManager.KillProcessAsync(executionId);
            tracker.State = Models.Domain.ExecutionState.Cancelled;

            _logger.LogInformation("Execution cancelled successfully: {ExecutionId}", executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel execution: {ExecutionId}", executionId);
            throw;
        }
    }

    // ========================================================================
    // TASK DISCOVERY
    // ========================================================================

    public async Task<IEnumerable<BoltTask>> ListTasksAsync()
    {
        _logger.LogInformation("Listing available Bolt tasks");

        try
        {
            var boltArgs = new[] { "task", "show", "--format", "json" };

            var processResult = await _processManager.ExecuteAsync(
                _boltExecutablePath,
                boltArgs,
                timeoutSeconds: 30);

            if (!processResult.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to list tasks. Exit code: {processResult.ExitCode}, Error: {processResult.StandardError}");
            }

            var tasks = ParseTaskList(processResult.StandardOutput);

            _logger.LogInformation("Found {TaskCount} available tasks", tasks.Count());

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Bolt tasks");
            throw;
        }
    }

    public async Task<BoltTaskDetails> GetTaskDetailsAsync(string taskName)
    {
        if (string.IsNullOrWhiteSpace(taskName))
            throw new ArgumentException("Task name cannot be null or empty", nameof(taskName));

        _logger.LogInformation("Getting details for task: {TaskName}", taskName);

        try
        {
            var boltArgs = new[] { "task", "show", taskName, "--format", "json" };

            var processResult = await _processManager.ExecuteAsync(
                _boltExecutablePath,
                boltArgs,
                timeoutSeconds: 30);

            if (!processResult.Success)
            {
                if (processResult.StandardError.Contains("Could not find") ||
                    processResult.StandardError.Contains("not found"))
                {
                    throw new KeyNotFoundException($"Task '{taskName}' not found");
                }

                throw new InvalidOperationException(
                    $"Failed to get task details. Exit code: {processResult.ExitCode}, Error: {processResult.StandardError}");
            }

            var taskDetails = ParseTaskDetails(processResult.StandardOutput, taskName);

            _logger.LogInformation("Retrieved details for task: {TaskName}", taskName);

            return taskDetails;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Failed to get task details for: {TaskName}", taskName);
            throw;
        }
    }

    // ========================================================================
    // PLAN DISCOVERY
    // ========================================================================

    public async Task<IEnumerable<BoltPlan>> ListPlansAsync()
    {
        _logger.LogInformation("Listing available Bolt plans");

        try
        {
            var boltArgs = new[] { "plan", "show", "--format", "json" };

            var processResult = await _processManager.ExecuteAsync(
                _boltExecutablePath,
                boltArgs,
                timeoutSeconds: 30);

            if (!processResult.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to list plans. Exit code: {processResult.ExitCode}, Error: {processResult.StandardError}");
            }

            var plans = ParsePlanList(processResult.StandardOutput);

            _logger.LogInformation("Found {PlanCount} available plans", plans.Count());

            return plans;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Bolt plans");
            throw;
        }
    }

    public async Task<BoltPlanDetails> GetPlanDetailsAsync(string planName)
    {
        if (string.IsNullOrWhiteSpace(planName))
            throw new ArgumentException("Plan name cannot be null or empty", nameof(planName));

        _logger.LogInformation("Getting details for plan: {PlanName}", planName);

        try
        {
            var boltArgs = new[] { "plan", "show", planName, "--format", "json" };

            var processResult = await _processManager.ExecuteAsync(
                _boltExecutablePath,
                boltArgs,
                timeoutSeconds: 30);

            if (!processResult.Success)
            {
                if (processResult.StandardError.Contains("Could not find") ||
                    processResult.StandardError.Contains("not found"))
                {
                    throw new KeyNotFoundException($"Plan '{planName}' not found");
                }

                throw new InvalidOperationException(
                    $"Failed to get plan details. Exit code: {processResult.ExitCode}, Error: {processResult.StandardError}");
            }

            var planDetails = ParsePlanDetails(processResult.StandardOutput, planName);

            _logger.LogInformation("Retrieved details for plan: {PlanName}", planName);

            return planDetails;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "Failed to get plan details for: {PlanName}", planName);
            throw;
        }
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    private Models.Entities.ExecutionState ConvertToEntityState(Models.Domain.ExecutionState domainState)
    {
        return domainState switch
        {
            Models.Domain.ExecutionState.Queued => Models.Entities.ExecutionState.Queued,
            Models.Domain.ExecutionState.Running => Models.Entities.ExecutionState.Running,
            Models.Domain.ExecutionState.Completed => Models.Entities.ExecutionState.Completed,
            Models.Domain.ExecutionState.Failed => Models.Entities.ExecutionState.Failed,
            Models.Domain.ExecutionState.Cancelled => Models.Entities.ExecutionState.Cancelled,
            _ => Models.Entities.ExecutionState.Failed
        };
    }

    private Models.Domain.ExecutionState ConvertToDomainState(Models.Entities.ExecutionState entityState)
    {
        return entityState switch
        {
            Models.Entities.ExecutionState.Queued => Models.Domain.ExecutionState.Queued,
            Models.Entities.ExecutionState.Running => Models.Domain.ExecutionState.Running,
            Models.Entities.ExecutionState.Completed => Models.Domain.ExecutionState.Completed,
            Models.Entities.ExecutionState.Failed => Models.Domain.ExecutionState.Failed,
            Models.Entities.ExecutionState.Cancelled => Models.Domain.ExecutionState.Cancelled,
            _ => Models.Domain.ExecutionState.Failed
        };
    }

    private async Task LogExecutionToDatabase(
        Guid executionId,
        string userId,
        ExecutionType type,
        string command,
        string arguments,
        DateTime startedAt,
        DateTime completedAt,
        TimeSpan executionTime,
        int exitCode,
        string standardOutput,
        string standardError,
        bool success,
        Models.Entities.ExecutionState state)
    {
        try
        {
            var user = await _dbContext.Users.FindAsync(userId);
            var username = user?.Username ?? "Unknown";

            var historyEntry = new ExecutionHistoryEntity
            {
                Id = executionId,
                UserId = userId,
                Username = username,
                Type = type,
                Command = command,
                Arguments = arguments,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                ExecutionTime = executionTime,
                ExitCode = exitCode,
                StandardOutput = standardOutput,
                StandardError = standardError,
                Success = success,
                State = state
            };

            _dbContext.ExecutionHistory.Add(historyEntry);
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Execution logged to database: {ExecutionId}", executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log execution to database: {ExecutionId}", executionId);
            // Don't throw - logging failure shouldn't fail the execution
        }
    }

    private List<NodeResult> ParseTaskResults(string jsonOutput)
    {
        var results = new List<NodeResult>();

        try
        {
            if (string.IsNullOrWhiteSpace(jsonOutput))
                return results;

            using var document = JsonDocument.Parse(jsonOutput);

            // Bolt task results are typically in an array of node results
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    var nodeResult = new NodeResult
                    {
                        NodeName = element.TryGetProperty("target", out var target)
                            ? target.GetString() ?? "unknown"
                            : "unknown",
                        Success = element.TryGetProperty("status", out var status)
                            ? status.GetString() == "success"
                            : false,
                        Result = element.TryGetProperty("result", out var result)
                            ? JsonSerializer.Deserialize<object>(result.GetRawText())
                            : null,
                        Error = element.TryGetProperty("error", out var error)
                            ? error.GetString()
                            : null
                    };
                    results.Add(nodeResult);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse task results as JSON, returning empty results");
        }

        return results;
    }

    private object? ParsePlanReturnValue(string jsonOutput)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonOutput))
                return null;

            using var document = JsonDocument.Parse(jsonOutput);

            if (document.RootElement.TryGetProperty("return", out var returnValue))
            {
                return JsonSerializer.Deserialize<object>(returnValue.GetRawText());
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse plan return value");
        }

        return null;
    }

    private IEnumerable<BoltTask> ParseTaskList(string jsonOutput)
    {
        var tasks = new List<BoltTask>();

        try
        {
            if (string.IsNullOrWhiteSpace(jsonOutput))
                return tasks;

            using var document = JsonDocument.Parse(jsonOutput);

            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    var task = new BoltTask
                    {
                        Name = element.TryGetProperty("name", out var name)
                            ? name.GetString() ?? string.Empty
                            : string.Empty,
                        Description = element.TryGetProperty("description", out var desc)
                            ? desc.GetString() ?? string.Empty
                            : string.Empty,
                        Module = element.TryGetProperty("module", out var mod)
                            ? mod.GetString() ?? string.Empty
                            : string.Empty
                    };
                    tasks.Add(task);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse task list");
        }

        return tasks;
    }

    private BoltTaskDetails ParseTaskDetails(string jsonOutput, string taskName)
    {
        var taskDetails = new BoltTaskDetails
        {
            Name = taskName,
            Parameters = new Dictionary<string, TaskParameter>()
        };

        try
        {
            if (string.IsNullOrWhiteSpace(jsonOutput))
                return taskDetails;

            using var document = JsonDocument.Parse(jsonOutput);

            taskDetails.Description = document.RootElement.TryGetProperty("description", out var desc)
                ? desc.GetString() ?? string.Empty
                : string.Empty;

            taskDetails.Module = document.RootElement.TryGetProperty("module", out var mod)
                ? mod.GetString() ?? string.Empty
                : string.Empty;

            if (document.RootElement.TryGetProperty("parameters", out var parameters))
            {
                foreach (var param in parameters.EnumerateObject())
                {
                    var paramDetails = new TaskParameter
                    {
                        Type = param.Value.TryGetProperty("type", out var type)
                            ? type.GetString() ?? "String"
                            : "String",
                        Description = param.Value.TryGetProperty("description", out var paramDesc)
                            ? paramDesc.GetString() ?? string.Empty
                            : string.Empty,
                        Required = param.Value.TryGetProperty("required", out var required) && required.GetBoolean(),
                        Default = param.Value.TryGetProperty("default", out var defaultVal)
                            ? JsonSerializer.Deserialize<object>(defaultVal.GetRawText())
                            : null
                    };
                    taskDetails.Parameters[param.Name] = paramDetails;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse task details for: {TaskName}", taskName);
        }

        return taskDetails;
    }

    private IEnumerable<BoltPlan> ParsePlanList(string jsonOutput)
    {
        var plans = new List<BoltPlan>();

        try
        {
            if (string.IsNullOrWhiteSpace(jsonOutput))
                return plans;

            using var document = JsonDocument.Parse(jsonOutput);

            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    var plan = new BoltPlan
                    {
                        Name = element.TryGetProperty("name", out var name)
                            ? name.GetString() ?? string.Empty
                            : string.Empty,
                        Description = element.TryGetProperty("description", out var desc)
                            ? desc.GetString() ?? string.Empty
                            : string.Empty,
                        Module = element.TryGetProperty("module", out var mod)
                            ? mod.GetString() ?? string.Empty
                            : string.Empty
                    };
                    plans.Add(plan);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse plan list");
        }

        return plans;
    }

    private BoltPlanDetails ParsePlanDetails(string jsonOutput, string planName)
    {
        var planDetails = new BoltPlanDetails
        {
            Name = planName,
            Parameters = new Dictionary<string, PlanParameter>()
        };

        try
        {
            if (string.IsNullOrWhiteSpace(jsonOutput))
                return planDetails;

            using var document = JsonDocument.Parse(jsonOutput);

            planDetails.Description = document.RootElement.TryGetProperty("description", out var desc)
                ? desc.GetString() ?? string.Empty
                : string.Empty;

            planDetails.Module = document.RootElement.TryGetProperty("module", out var mod)
                ? mod.GetString() ?? string.Empty
                : string.Empty;

            if (document.RootElement.TryGetProperty("parameters", out var parameters))
            {
                foreach (var param in parameters.EnumerateObject())
                {
                    var paramDetails = new PlanParameter
                    {
                        Type = param.Value.TryGetProperty("type", out var type)
                            ? type.GetString() ?? "String"
                            : "String",
                        Description = param.Value.TryGetProperty("description", out var paramDesc)
                            ? paramDesc.GetString() ?? string.Empty
                            : string.Empty,
                        Required = param.Value.TryGetProperty("required", out var required) && required.GetBoolean(),
                        Default = param.Value.TryGetProperty("default", out var defaultVal)
                            ? JsonSerializer.Deserialize<object>(defaultVal.GetRawText())
                            : null
                    };
                    planDetails.Parameters[param.Name] = paramDetails;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse plan details for: {PlanName}", planName);
        }

        return planDetails;
    }

    // ========================================================================
    // SIGNALR NOTIFICATIONS
    // ========================================================================

    private async Task SendExecutionStartedNotificationAsync(
        Guid executionId,
        string executionType,
        string executionName,
        string userId,
        string username,
        string? targets = null)
    {
        try
        {
            var startedEvent = new ExecutionStartedEvent
            {
                ExecutionId = executionId,
                ExecutionType = executionType,
                ExecutionName = executionName,
                UserId = userId,
                Username = username,
                State = "Running",
                StartedAt = DateTime.UtcNow,
                Targets = targets
            };

            await _hubContext.Clients.Group($"execution_{executionId}").SendAsync("ExecutionStarted", startedEvent);
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ExecutionStarted", startedEvent);
            await _hubContext.Clients.Group("all_executions").SendAsync("ExecutionStarted", startedEvent);

            _logger.LogDebug("Sent ExecutionStarted notification for {ExecutionId}", executionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send ExecutionStarted notification for {ExecutionId}", executionId);
        }
    }

    private async Task SendExecutionOutputNotificationAsync(
        Guid executionId,
        string outputType,
        string output,
        string userId)
    {
        try
        {
            var outputEvent = new ExecutionOutputEvent
            {
                ExecutionId = executionId,
                OutputType = outputType,
                Output = output,
                UserId = userId
            };

            await _hubContext.Clients.Group($"execution_{executionId}").SendAsync("ExecutionOutput", outputEvent);

            _logger.LogTrace("Sent ExecutionOutput notification for {ExecutionId}: {OutputType}", executionId, outputType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send ExecutionOutput notification for {ExecutionId}", executionId);
        }
    }

    private async Task SendExecutionCompletedNotificationAsync(
        Guid executionId,
        string executionType,
        string executionName,
        string userId,
        string username,
        bool success,
        long durationMs,
        int? exitCode = null,
        string? summary = null)
    {
        try
        {
            var completedEvent = new ExecutionCompletedEvent
            {
                ExecutionId = executionId,
                ExecutionType = executionType,
                ExecutionName = executionName,
                UserId = userId,
                Username = username,
                CompletedAt = DateTime.UtcNow,
                DurationMs = durationMs,
                ExitCode = exitCode,
                Success = success,
                Summary = summary
            };

            await _hubContext.Clients.Group($"execution_{executionId}").SendAsync("ExecutionCompleted", completedEvent);
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ExecutionCompleted", completedEvent);
            await _hubContext.Clients.Group("all_executions").SendAsync("ExecutionCompleted", completedEvent);

            _logger.LogDebug("Sent ExecutionCompleted notification for {ExecutionId}", executionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send ExecutionCompleted notification for {ExecutionId}", executionId);
        }
    }

    private async Task SendExecutionFailedNotificationAsync(
        Guid executionId,
        string executionType,
        string executionName,
        string userId,
        string username,
        string errorMessage,
        int? exitCode = null,
        string? errorDetails = null)
    {
        try
        {
            var failedEvent = new ExecutionFailedEvent
            {
                ExecutionId = executionId,
                ExecutionType = executionType,
                ExecutionName = executionName,
                UserId = userId,
                Username = username,
                FailedAt = DateTime.UtcNow,
                ErrorMessage = errorMessage,
                ExitCode = exitCode,
                ErrorDetails = errorDetails
            };

            await _hubContext.Clients.Group($"execution_{executionId}").SendAsync("ExecutionFailed", failedEvent);
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ExecutionFailed", failedEvent);
            await _hubContext.Clients.Group("all_executions").SendAsync("ExecutionFailed", failedEvent);

            _logger.LogDebug("Sent ExecutionFailed notification for {ExecutionId}", executionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send ExecutionFailed notification for {ExecutionId}", executionId);
        }
    }

    private async Task SendExecutionCancelledNotificationAsync(
        Guid executionId,
        string executionType,
        string executionName,
        string userId,
        string username,
        string? cancelledByUserId = null,
        string? cancelledByUsername = null)
    {
        try
        {
            var cancelledEvent = new ExecutionCancelledEvent
            {
                ExecutionId = executionId,
                ExecutionType = executionType,
                ExecutionName = executionName,
                UserId = userId,
                Username = username,
                CancelledAt = DateTime.UtcNow,
                CancelledByUserId = cancelledByUserId,
                CancelledByUsername = cancelledByUsername
            };

            await _hubContext.Clients.Group($"execution_{executionId}").SendAsync("ExecutionCancelled", cancelledEvent);
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ExecutionCancelled", cancelledEvent);
            await _hubContext.Clients.Group("all_executions").SendAsync("ExecutionCancelled", cancelledEvent);

            _logger.LogDebug("Sent ExecutionCancelled notification for {ExecutionId}", executionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send ExecutionCancelled notification for {ExecutionId}", executionId);
        }
    }

    // ========================================================================
    // INTERNAL CLASSES
    // ========================================================================

    private class ExecutionTracker
    {
        public Guid ExecutionId { get; set; }
        public Models.Domain.ExecutionState State { get; set; }
        public StringBuilder Output { get; set; } = new();
        public StringBuilder ErrorOutput { get; set; } = new();
        public DateTime StartTime { get; set; }
    }
}
