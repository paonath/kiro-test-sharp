# Claude Implementation Plan - Puppet Bolt Web API

**Generated:** 2025-10-23
**Branch:** feature/claude_code
**Status:** Active Development

## Executive Summary

This document provides a comprehensive implementation plan for completing the Puppet Bolt Web API based on the specifications in `.kiro/specs/puppet-bolt-web-interface/` and the current codebase analysis. The project is approximately **30% complete**, with core authentication, database, and process management infrastructure in place.

### What's Already Implemented ✅

- **Authentication System** (100%): JWT authentication, login/logout/refresh endpoints, BCrypt password hashing
- **Database Layer** (100%): EF Core with all entities, migrations, and Fluent API configuration
- **Process Manager** (100%): Singleton service for CLI execution with timeout, concurrency limiting, and real-time output capture
- **Project Infrastructure** (100%): ASP.NET Core 8.0, Minimal API setup, Swagger, Serilog, CORS, DI container

### What Needs Implementation 🚧

- **Bolt Execution Service** (0%): Core service to execute Bolt commands/tasks/plans
- **Inventory Service** (0%): Parse and manage Bolt inventory files
- **Configuration Service** (0%): Manage Bolt configuration files
- **Execution History Service** (0%): Track and retrieve command history
- **Endpoint Groups** (0%): BoltCommand, BoltTask, BoltPlan, Inventory, Configuration, History endpoints
- **SignalR Hub** (0%): Real-time command output streaming
- **Validators** (20%): Missing validators for Bolt-specific requests
- **Middleware** (0%): Error handling and rate limiting middleware

---

## Implementation Phases

This plan is organized into **6 phases**, building incrementally from core services to complete API functionality.

---

## PHASE 1: Core Bolt Execution Service

**Priority:** CRITICAL
**Dependencies:** ProcessManager (already implemented)
**Estimated Complexity:** HIGH

### 1.1 Create Domain Models for Bolt Operations

**File:** `Models/Domain/BoltModels.cs`

```csharp
// Request Models
public class CommandExecutionRequest
{
    public string Command { get; set; }
    public string[] Arguments { get; set; }
    public int? TimeoutSeconds { get; set; }
}

public class TaskExecutionRequest
{
    public string TaskName { get; set; }
    public string[] Targets { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public int? TimeoutSeconds { get; set; }
}

public class PlanExecutionRequest
{
    public string PlanName { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public int? TimeoutSeconds { get; set; }
}

// Response Models
public class CommandExecutionResult
{
    public Guid ExecutionId { get; set; }
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; }
    public string StandardError { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public bool Success { get; set; }
}

public class TaskExecutionResult
{
    public Guid ExecutionId { get; set; }
    public string TaskName { get; set; }
    public List<NodeResult> NodeResults { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public bool Success { get; set; }
}

public class NodeResult
{
    public string NodeName { get; set; }
    public bool Success { get; set; }
    public object Result { get; set; }
    public string Error { get; set; }
}

public class PlanExecutionResult
{
    public Guid ExecutionId { get; set; }
    public string PlanName { get; set; }
    public object ReturnValue { get; set; }
    public string Output { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public bool Success { get; set; }
}

public class ExecutionStatus
{
    public Guid ExecutionId { get; set; }
    public ExecutionState State { get; set; }
    public string CurrentOutput { get; set; }
    public DateTime StartedAt { get; set; }
    public TimeSpan ElapsedTime { get; set; }
}

// Supporting Models
public class BoltTask
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Module { get; set; }
}

public class BoltTaskDetails : BoltTask
{
    public Dictionary<string, TaskParameter> Parameters { get; set; }
}

public class TaskParameter
{
    public string Type { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public object Default { get; set; }
}

public class BoltPlan
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Module { get; set; }
}

public class BoltPlanDetails : BoltPlan
{
    public Dictionary<string, PlanParameter> Parameters { get; set; }
}

public class PlanParameter
{
    public string Type { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public object Default { get; set; }
}
```

**Requirements Met:** 1.1, 1.2, 1.3, 3.2, 3.3, 3.5, 4.2, 4.3, 4.5

---

### 1.2 Create IBoltExecutionService Interface

**File:** `Services/Interfaces/IBoltExecutionService.cs`

```csharp
public interface IBoltExecutionService
{
    // Command Execution
    Task<CommandExecutionResult> ExecuteCommandAsync(
        string command,
        string[] arguments,
        string userId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);

    // Task Execution
    Task<TaskExecutionResult> ExecuteTaskAsync(
        string taskName,
        string[] targets,
        Dictionary<string, object> parameters,
        string userId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);

    // Plan Execution
    Task<PlanExecutionResult> ExecutePlanAsync(
        string planName,
        Dictionary<string, object> parameters,
        string userId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);

    // Execution Management
    Task<ExecutionStatus> GetExecutionStatusAsync(Guid executionId);
    Task CancelExecutionAsync(Guid executionId);

    // Task/Plan Discovery
    Task<IEnumerable<BoltTask>> ListTasksAsync();
    Task<BoltTaskDetails> GetTaskDetailsAsync(string taskName);
    Task<IEnumerable<BoltPlan>> ListPlansAsync();
    Task<BoltPlanDetails> GetPlanDetailsAsync(string planName);
}
```

**Requirements Met:** 1.1, 3.1, 3.2, 4.1, 4.2, 8.1, 8.2, 8.4

---

### 1.3 Implement BoltExecutionService

**File:** `Services/Implementations/BoltExecutionService.cs`

**Key Implementation Details:**

1. **Constructor Dependencies:**
   - `IProcessManager` - For CLI execution
   - `IConfiguration` - For Bolt executable path and settings
   - `ILogger<BoltExecutionService>` - For logging
   - `BoltDbContext` - For execution history tracking

2. **Internal State:**
   - `ConcurrentDictionary<Guid, ExecutionTracker>` - Track active executions
   - `ExecutionTracker` class with: `ExecutionId, State, StringBuilder Output, StartTime`

3. **ExecuteCommandAsync Implementation:**
   ```csharp
   - Build Bolt CLI command: $"{boltPath} command run {command} {argumentsString} --format json"
   - Create execution tracker entry
   - Call IProcessManager.ExecuteAsync with:
     - onStdOut callback: Append to tracker's output buffer
     - onStdErr callback: Append to error buffer
   - Parse JSON output from Bolt
   - Log execution to ExecutionHistoryEntity
   - Return CommandExecutionResult
   ```

4. **ExecuteTaskAsync Implementation:**
   ```csharp
   - Build Bolt CLI command: $"{boltPath} task run {taskName} --targets {targetsList} --params '{paramsJson}' --format json"
   - Execute via ProcessManager
   - Parse JSON output to extract per-node results
   - Map to TaskExecutionResult with NodeResult list
   - Log to database
   ```

5. **ExecutePlanAsync Implementation:**
   ```csharp
   - Build Bolt CLI command: $"{boltPath} plan run {planName} --params '{paramsJson}' --format json"
   - Execute via ProcessManager
   - Parse JSON output for return value and status
   - Map to PlanExecutionResult
   - Log to database
   ```

6. **ListTasksAsync Implementation:**
   ```csharp
   - Execute: $"{boltPath} task show --format json"
   - Parse JSON array of tasks
   - Return List<BoltTask>
   ```

7. **GetTaskDetailsAsync Implementation:**
   ```csharp
   - Execute: $"{boltPath} task show {taskName} --format json"
   - Parse JSON for task metadata including parameters
   - Return BoltTaskDetails
   ```

8. **ListPlansAsync & GetPlanDetailsAsync:**
   - Similar pattern to tasks using "plan show" commands

9. **GetExecutionStatusAsync:**
   ```csharp
   - Look up execution in ConcurrentDictionary
   - If not found, query ExecutionHistoryEntity for historical executions
   - Return current state and accumulated output
   ```

10. **CancelExecutionAsync:**
    ```csharp
    - Delegate to IProcessManager.KillProcessAsync
    - Update execution tracker state to Cancelled
    - Log cancellation
    ```

**Error Handling:**
- Catch process execution errors
- Parse Bolt CLI stderr for specific error messages
- Throw appropriate exceptions (InvalidOperationException, ArgumentException, etc.)

**Requirements Met:** 1.1, 1.2, 1.3, 1.4, 1.5, 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 4.5, 8.1, 8.2, 8.4, 8.5

**Service Lifetime:** Scoped (per request, but maintains in-memory state via Singleton pattern internally)

**Register in Program.cs:**
```csharp
builder.Services.AddScoped<IBoltExecutionService, BoltExecutionService>();
```

---

### 1.4 Create Validators for Bolt Requests

**Files:**

**`Validators/CommandExecutionRequestValidator.cs`**
```csharp
public class CommandExecutionRequestValidator : AbstractValidator<CommandExecutionRequest>
{
    public CommandExecutionRequestValidator()
    {
        RuleFor(x => x.Command)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Command must not exceed 500 characters");

        RuleFor(x => x.Arguments)
            .NotNull()
            .WithMessage("Arguments array is required (can be empty)");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(3600)
            .When(x => x.TimeoutSeconds.HasValue)
            .WithMessage("Timeout must be between 1 and 3600 seconds");

        // Security: Validate command doesn't contain injection attempts
        RuleFor(x => x.Command)
            .Must(cmd => !cmd.Contains("&&") && !cmd.Contains("||") && !cmd.Contains(";"))
            .WithMessage("Command contains potentially unsafe characters");
    }
}
```

**`Validators/TaskExecutionRequestValidator.cs`**
```csharp
public class TaskExecutionRequestValidator : AbstractValidator<TaskExecutionRequest>
{
    public TaskExecutionRequestValidator()
    {
        RuleFor(x => x.TaskName)
            .NotEmpty()
            .Matches(@"^[a-zA-Z0-9_:]+$")
            .WithMessage("Task name must contain only alphanumeric characters, underscores, and colons");

        RuleFor(x => x.Targets)
            .NotEmpty()
            .WithMessage("At least one target must be specified");

        RuleFor(x => x.Parameters)
            .NotNull();

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(3600)
            .When(x => x.TimeoutSeconds.HasValue);
    }
}
```

**`Validators/PlanExecutionRequestValidator.cs`**
```csharp
public class PlanExecutionRequestValidator : AbstractValidator<PlanExecutionRequest>
{
    public PlanExecutionRequestValidator()
    {
        RuleFor(x => x.PlanName)
            .NotEmpty()
            .Matches(@"^[a-zA-Z0-9_:]+$")
            .WithMessage("Plan name must contain only alphanumeric characters, underscores, and colons");

        RuleFor(x => x.Parameters)
            .NotNull();

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(3600)
            .When(x => x.TimeoutSeconds.HasValue);
    }
}
```

**Requirements Met:** 1.1, 3.2, 3.4, 4.2, 4.4

---

## PHASE 2: Execution Endpoints

**Priority:** CRITICAL
**Dependencies:** BoltExecutionService (Phase 1)
**Estimated Complexity:** MEDIUM

### 2.1 Implement BoltCommandEndpoints

**File:** `Endpoints/BoltCommandEndpoints.cs`

```csharp
public static class BoltCommandEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bolt/commands")
            .RequireAuthorization()
            .WithTags("Bolt Commands");

        group.MapPost("/execute", ExecuteCommand)
            .WithName("ExecuteCommand")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Execute a Bolt command";
                operation.Description = "Executes an arbitrary Bolt command with specified arguments";
                return operation;
            })
            .Produces<CommandExecutionResult>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapGet("/{executionId:guid}/status", GetExecutionStatus)
            .WithName("GetExecutionStatus")
            .WithOpenApi()
            .Produces<ExecutionStatus>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{executionId:guid}/cancel", CancelExecution)
            .WithName("CancelExecution")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<CommandExecutionResult>, BadRequest<ErrorResponse>, UnauthorizedHttpResult>>
        ExecuteCommand(
            CommandExecutionRequest request,
            IValidator<CommandExecutionRequest> validator,
            IBoltExecutionService service,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltCommandEndpoints");

        // Validate request
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors
            });
        }

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await service.ExecuteCommandAsync(
                request.Command,
                request.Arguments,
                userId,
                request.TimeoutSeconds);

            logger.LogInformation(
                "Command executed successfully. ExecutionId: {ExecutionId}, ExitCode: {ExitCode}",
                result.ExecutionId,
                result.ExitCode);

            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute command: {Command}", request.Command);
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "Command execution failed",
                ErrorCode = "EXECUTION_ERROR"
            });
        }
    }

    private static async Task<Results<Ok<ExecutionStatus>, NotFound<ErrorResponse>>>
        GetExecutionStatus(
            Guid executionId,
            IBoltExecutionService service,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltCommandEndpoints");

        try
        {
            var status = await service.GetExecutionStatusAsync(executionId);
            return TypedResults.Ok(status);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Execution not found: {ExecutionId}", executionId);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = "Execution not found",
                ErrorCode = "NOT_FOUND"
            });
        }
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>>
        CancelExecution(
            Guid executionId,
            IBoltExecutionService service,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltCommandEndpoints");

        try
        {
            await service.CancelExecutionAsync(executionId);
            logger.LogInformation("Execution cancelled: {ExecutionId}", executionId);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = "Execution not found",
                ErrorCode = "NOT_FOUND"
            });
        }
    }
}
```

**Register in Program.cs:**
```csharp
BoltCommandEndpoints.MapEndpoints(app);
```

**Requirements Met:** 1.1, 1.2, 1.3, 1.4, 8.2, 8.4, 8.5

---

### 2.2 Implement BoltTaskEndpoints

**File:** `Endpoints/BoltTaskEndpoints.cs`

```csharp
public static class BoltTaskEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bolt/tasks")
            .RequireAuthorization()
            .WithTags("Bolt Tasks");

        group.MapGet("/", ListTasks)
            .WithName("ListTasks")
            .WithOpenApi()
            .Produces<IEnumerable<BoltTask>>(StatusCodes.Status200OK);

        group.MapGet("/{taskName}", GetTaskDetails)
            .WithName("GetTaskDetails")
            .WithOpenApi()
            .Produces<BoltTaskDetails>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/execute", ExecuteTask)
            .WithName("ExecuteTask")
            .WithOpenApi()
            .Produces<TaskExecutionResult>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);
    }

    private static async Task<Ok<IEnumerable<BoltTask>>> ListTasks(
        IBoltExecutionService service,
        ILoggerFactory loggerFactory)
    {
        var tasks = await service.ListTasksAsync();
        return TypedResults.Ok(tasks);
    }

    private static async Task<Results<Ok<BoltTaskDetails>, NotFound<ErrorResponse>>>
        GetTaskDetails(
            string taskName,
            IBoltExecutionService service,
            ILoggerFactory loggerFactory)
    {
        try
        {
            var details = await service.GetTaskDetailsAsync(taskName);
            return TypedResults.Ok(details);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Task '{taskName}' not found",
                ErrorCode = "TASK_NOT_FOUND"
            });
        }
    }

    private static async Task<Results<Ok<TaskExecutionResult>, BadRequest<ErrorResponse>>>
        ExecuteTask(
            TaskExecutionRequest request,
            IValidator<TaskExecutionRequest> validator,
            IBoltExecutionService service,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltTaskEndpoints");

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors
            });
        }

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await service.ExecuteTaskAsync(
                request.TaskName,
                request.Targets,
                request.Parameters,
                userId,
                request.TimeoutSeconds);

            logger.LogInformation(
                "Task executed successfully. ExecutionId: {ExecutionId}, Task: {TaskName}",
                result.ExecutionId,
                result.TaskName);

            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute task: {TaskName}", request.TaskName);
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "Task execution failed",
                ErrorCode = "EXECUTION_ERROR"
            });
        }
    }
}
```

**Register in Program.cs:**
```csharp
BoltTaskEndpoints.MapEndpoints(app);
```

**Requirements Met:** 3.1, 3.2, 3.3, 3.4, 3.5

---

### 2.3 Implement BoltPlanEndpoints

**File:** `Endpoints/BoltPlanEndpoints.cs`

**Implementation:** Similar structure to BoltTaskEndpoints with:
- `GET /api/bolt/plans` - List all plans
- `GET /api/bolt/plans/{planName}` - Get plan details
- `POST /api/bolt/plans/execute` - Execute plan

**Requirements Met:** 4.1, 4.2, 4.3, 4.4, 4.5

---

## PHASE 3: Inventory & Configuration Services

**Priority:** HIGH
**Dependencies:** None (file system operations)
**Estimated Complexity:** MEDIUM

### 3.1 Create Inventory Models

**File:** `Models/Domain/InventoryModels.cs`

```csharp
public class BoltInventory
{
    public int Version { get; set; }
    public List<InventoryGroup> Groups { get; set; }
    public List<Node> Nodes { get; set; }
}

public class InventoryGroup
{
    public string Name { get; set; }
    public List<Node> Nodes { get; set; }
    public Dictionary<string, object> Config { get; set; }
}

public class Node
{
    public string Name { get; set; }
    public string Uri { get; set; }
    public Dictionary<string, object> Config { get; set; }
    public List<string> Groups { get; set; }
}

public class BoltConfiguration
{
    public string ModulePath { get; set; }
    public string InventoryFile { get; set; }
    public int Concurrency { get; set; }
    public Dictionary<string, object> Transport { get; set; }
    public Dictionary<string, object> AdditionalSettings { get; set; }
}
```

**Requirements Met:** 2.1, 2.2, 7.1

---

### 3.2 Create IInventoryService Interface

**File:** `Services/Interfaces/IInventoryService.cs`

```csharp
public interface IInventoryService
{
    Task<BoltInventory> GetInventoryAsync();
    Task<IEnumerable<string>> GetGroupsAsync();
    Task<IEnumerable<Node>> GetNodesByGroupAsync(string groupName);
    Task<bool> ValidateInventoryAsync();
}
```

**Requirements Met:** 2.1, 2.2, 2.3, 2.4

---

### 3.3 Implement InventoryService

**File:** `Services/Implementations/InventoryService.cs`

**Key Implementation Details:**

1. **Constructor Dependencies:**
   - `IConfiguration` - For inventory file path
   - `ILogger<InventoryService>`

2. **GetInventoryAsync Implementation:**
   ```csharp
   - Read inventory file from Bolt:InventoryFile config path
   - Parse YAML using YamlDotNet library
   - Deserialize to BoltInventory model
   - Handle file not found, invalid YAML, schema validation errors
   ```

3. **GetGroupsAsync:**
   ```csharp
   - Load inventory
   - Return distinct group names
   ```

4. **GetNodesByGroupAsync:**
   ```csharp
   - Load inventory
   - Filter nodes where Groups contains groupName
   - Return filtered list
   ```

5. **ValidateInventoryAsync:**
   ```csharp
   - Attempt to load and parse inventory
   - Return true if successful, false if errors
   - Log validation errors
   ```

**NuGet Package Required:** `YamlDotNet` (for YAML parsing)

**Service Lifetime:** Scoped

**Register in Program.cs:**
```csharp
builder.Services.AddScoped<IInventoryService, InventoryService>();
```

**Requirements Met:** 2.1, 2.2, 2.3, 2.4

---

### 3.4 Create IConfigurationService Interface

**File:** `Services/Interfaces/IConfigurationService.cs`

```csharp
public interface IConfigurationService
{
    Task<BoltConfiguration> GetConfigurationAsync();
    Task UpdateConfigurationAsync(BoltConfiguration config, string userId);
    Task<ValidationResult> ValidateConfigurationAsync(BoltConfiguration config);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
}
```

**Requirements Met:** 7.1, 7.2, 7.3

---

### 3.5 Implement ConfigurationService

**File:** `Services/Implementations/ConfigurationService.cs`

**Key Implementation Details:**

1. **Constructor Dependencies:**
   - `IConfiguration` - For config file path
   - `BoltDbContext` - For logging configuration changes
   - `ILogger<ConfigurationService>`

2. **GetConfigurationAsync:**
   ```csharp
   - Read bolt-project.yaml from Bolt:ConfigFile path
   - Parse YAML to BoltConfiguration model
   - Return configuration
   ```

3. **UpdateConfigurationAsync:**
   ```csharp
   - Validate new configuration
   - Read current configuration
   - Log change to ConfigurationChangeEntity (audit trail)
   - Write new configuration to YAML file
   - Handle file write errors
   ```

4. **ValidateConfigurationAsync:**
   ```csharp
   - Check required fields (ModulePath, InventoryFile, Concurrency > 0)
   - Validate paths exist
   - Return ValidationResult with errors list
   ```

**Service Lifetime:** Scoped

**Register in Program.cs:**
```csharp
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
```

**Requirements Met:** 7.1, 7.2, 7.3, 7.4, 7.5

---

### 3.6 Implement InventoryEndpoints

**File:** `Endpoints/InventoryEndpoints.cs`

```csharp
public static class InventoryEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bolt/inventory")
            .RequireAuthorization()
            .WithTags("Inventory");

        group.MapGet("/", GetInventory)
            .Produces<BoltInventory>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/groups", GetGroups)
            .Produces<IEnumerable<string>>(StatusCodes.Status200OK);

        group.MapGet("/groups/{groupName}/nodes", GetNodesByGroup)
            .Produces<IEnumerable<Node>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    // Handler implementations (standard pattern as previous endpoints)
}
```

**Requirements Met:** 2.1, 2.2, 2.3, 2.4

---

### 3.7 Implement ConfigurationEndpoints

**File:** `Endpoints/ConfigurationEndpoints.cs`

```csharp
public static class ConfigurationEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bolt/config")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))  // Admin only
            .WithTags("Configuration");

        group.MapGet("/", GetConfiguration)
            .Produces<BoltConfiguration>(StatusCodes.Status200OK);

        group.MapPut("/", UpdateConfiguration)
            .Produces<BoltConfiguration>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/validate", ValidateConfiguration)
            .Produces<ValidationResult>(StatusCodes.Status200OK);
    }

    // Handler implementations
}
```

**Requirements Met:** 7.1, 7.2, 7.3, 7.4, 7.5

---

## PHASE 4: Execution History Service

**Priority:** MEDIUM
**Dependencies:** BoltExecutionService (Phase 1)
**Estimated Complexity:** LOW

### 4.1 Create History Models

**File:** `Models/Domain/HistoryModels.cs`

```csharp
public class ExecutionHistoryItem
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string Username { get; set; }
    public ExecutionType Type { get; set; }
    public string Command { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Success { get; set; }
    public ExecutionState State { get; set; }
}

public class ExecutionHistoryDetails : ExecutionHistoryItem
{
    public string Arguments { get; set; }
    public TimeSpan? ExecutionTime { get; set; }
    public int? ExitCode { get; set; }
    public string StandardOutput { get; set; }
    public string StandardError { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

**Requirements Met:** 6.2, 6.3, 6.5

---

### 4.2 Create IExecutionHistoryService Interface

**File:** `Services/Interfaces/IExecutionHistoryService.cs`

```csharp
public interface IExecutionHistoryService
{
    Task LogExecutionAsync(ExecutionHistoryItem item);

    Task<PagedResult<ExecutionHistoryItem>> GetHistoryAsync(
        int page,
        int pageSize,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string userId = null);

    Task<ExecutionHistoryDetails> GetExecutionDetailsAsync(Guid executionId);
}
```

**Requirements Met:** 6.1, 6.2, 6.3, 6.4, 6.5

---

### 4.3 Implement ExecutionHistoryService

**File:** `Services/Implementations/ExecutionHistoryService.cs`

**Key Implementation Details:**

1. **Constructor Dependencies:**
   - `BoltDbContext` - Database access
   - `ILogger<ExecutionHistoryService>`

2. **LogExecutionAsync:**
   ```csharp
   - Create ExecutionHistoryEntity from item
   - Add to DbContext.ExecutionHistory
   - SaveChangesAsync
   ```

3. **GetHistoryAsync:**
   ```csharp
   - Query ExecutionHistory with filters (date range, userId)
   - Order by StartedAt descending
   - Apply pagination (Skip/Take)
   - Project to ExecutionHistoryItem
   - Return PagedResult
   ```

4. **GetExecutionDetailsAsync:**
   ```csharp
   - Query ExecutionHistory by Id
   - Include all fields (output, errors, etc.)
   - Project to ExecutionHistoryDetails
   - Throw KeyNotFoundException if not found
   ```

**Service Lifetime:** Scoped

**Register in Program.cs:**
```csharp
builder.Services.AddScoped<IExecutionHistoryService, ExecutionHistoryService>();
```

**Requirements Met:** 6.1, 6.2, 6.3, 6.4, 6.5

---

### 4.4 Implement HistoryEndpoints

**File:** `Endpoints/HistoryEndpoints.cs`

```csharp
public static class HistoryEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bolt/history")
            .RequireAuthorization()
            .WithTags("History");

        group.MapGet("/", GetHistory)
            .Produces<PagedResult<ExecutionHistoryItem>>(StatusCodes.Status200OK);

        group.MapGet("/{executionId:guid}", GetExecutionDetails)
            .Produces<ExecutionHistoryDetails>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<PagedResult<ExecutionHistoryItem>>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string userId = null,
        IExecutionHistoryService service)
    {
        var result = await service.GetHistoryAsync(page, pageSize, startDate, endDate, userId);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<ExecutionHistoryDetails>, NotFound<ErrorResponse>>>
        GetExecutionDetails(
            Guid executionId,
            IExecutionHistoryService service)
    {
        try
        {
            var details = await service.GetExecutionDetailsAsync(executionId);
            return TypedResults.Ok(details);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = "Execution not found",
                ErrorCode = "NOT_FOUND"
            });
        }
    }
}
```

**Requirements Met:** 6.1, 6.2, 6.3, 6.4, 6.5

---

## PHASE 5: Real-Time Features (SignalR)

**Priority:** LOW
**Dependencies:** BoltExecutionService (Phase 1)
**Estimated Complexity:** MEDIUM

### 5.1 Create ExecutionHub

**File:** `Hubs/ExecutionHub.cs`

```csharp
[Authorize]
public class ExecutionHub : Hub
{
    private readonly ILogger<ExecutionHub> _logger;

    public ExecutionHub(ILogger<ExecutionHub> logger)
    {
        _logger = logger;
    }

    public async Task SubscribeToExecution(Guid executionId)
    {
        var groupName = $"execution-{executionId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Client {ConnectionId} subscribed to execution {ExecutionId}",
            Context.ConnectionId,
            executionId);
    }

    public async Task UnsubscribeFromExecution(Guid executionId)
    {
        var groupName = $"execution-{executionId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from execution {ExecutionId}",
            Context.ConnectionId,
            executionId);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation(
            "Client {ConnectionId} disconnected",
            Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

// Client interface (for strongly-typed clients)
public interface IExecutionClient
{
    Task OnOutputReceived(Guid executionId, string output, string stream);
    Task OnExecutionCompleted(Guid executionId, CommandExecutionResult result);
    Task OnExecutionFailed(Guid executionId, string error);
}
```

**Map in Program.cs:**
```csharp
app.MapHub<ExecutionHub>("/hubs/execution");
```

**Requirements Met:** 8.1, 8.2

---

### 5.2 Integrate SignalR with BoltExecutionService

**Modification:** `Services/Implementations/BoltExecutionService.cs`

**Add Constructor Dependency:**
```csharp
private readonly IHubContext<ExecutionHub, IExecutionClient> _hubContext;

public BoltExecutionService(
    IProcessManager processManager,
    IConfiguration configuration,
    ILogger<BoltExecutionService> logger,
    BoltDbContext dbContext,
    IHubContext<ExecutionHub, IExecutionClient> hubContext)
{
    _hubContext = hubContext;
    // ...
}
```

**Modify ExecuteCommandAsync:**
```csharp
var result = await _processManager.ExecuteAsync(
    boltPath,
    args,
    onStdOut: async output =>
    {
        // Send to SignalR clients
        await _hubContext.Clients.Group($"execution-{executionId}")
            .OnOutputReceived(executionId, output, "stdout");
    },
    onStdErr: async output =>
    {
        await _hubContext.Clients.Group($"execution-{executionId}")
            .OnOutputReceived(executionId, output, "stderr");
    },
    timeoutSeconds,
    cancellationToken);

// After execution completes
await _hubContext.Clients.Group($"execution-{executionId}")
    .OnExecutionCompleted(executionId, commandResult);
```

**Requirements Met:** 8.2, 8.3

---

## PHASE 6: Middleware & Polish

**Priority:** MEDIUM
**Dependencies:** All endpoints implemented
**Estimated Complexity:** LOW

### 6.1 Create Global Exception Handling Middleware

**File:** `Middleware/ExceptionHandlingMiddleware.cs`

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            await HandleExceptionAsync(context, ex, StatusCodes.Status401Unauthorized, "UNAUTHORIZED");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await HandleExceptionAsync(context, ex, StatusCodes.Status404NotFound, "NOT_FOUND");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument");
            await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest, "INVALID_ARGUMENT");
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Operation timeout");
            await HandleExceptionAsync(context, ex, StatusCodes.Status408RequestTimeout, "TIMEOUT");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR");
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        int statusCode,
        string errorCode)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            Message = _env.IsDevelopment() ? exception.Message : "An error occurred processing your request",
            ErrorCode = errorCode,
            TraceId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
```

**Register in Program.cs:**
```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Requirements Met:** 1.4, 2.3, 4.4, 7.3

---

### 6.2 Create Rate Limiting Middleware

**File:** `Middleware/RateLimitingMiddleware.cs`

```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly int _permitLimit;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _clients;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _permitLimit = config.GetValue<int>("RateLimiting:PermitLimit", 100);
        _window = TimeSpan.Parse(config.GetValue<string>("RateLimiting:Window", "00:01:00"));
        _clients = new ConcurrentDictionary<string, RateLimitInfo>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);

        var info = _clients.GetOrAdd(clientId, _ => new RateLimitInfo
        {
            WindowStart = DateTime.UtcNow,
            RequestCount = 0
        });

        lock (info)
        {
            if (DateTime.UtcNow - info.WindowStart > _window)
            {
                info.WindowStart = DateTime.UtcNow;
                info.RequestCount = 0;
            }

            info.RequestCount++;

            if (info.RequestCount > _permitLimit)
            {
                _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    Message = "Rate limit exceeded",
                    ErrorCode = "RATE_LIMIT_EXCEEDED"
                });
                return;
            }
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use authenticated user ID if available, otherwise IP address
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userId ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class RateLimitInfo
    {
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
    }
}
```

**Register in Program.cs:**
```csharp
app.UseMiddleware<RateLimitingMiddleware>();
```

**Requirements Met:** Security best practices

---

### 6.3 Database Seeding for Default Admin User

**File:** `Data/DbInitializer.cs`

```csharp
public static class DbInitializer
{
    public static async Task InitializeAsync(BoltDbContext context, IConfiguration config)
    {
        // Apply pending migrations
        await context.Database.MigrateAsync();

        // Check if default admin exists
        if (await context.Users.AnyAsync(u => u.Username == "admin"))
        {
            return; // Already seeded
        }

        // Create default admin user
        var adminUser = new UserEntity
        {
            Id = Guid.NewGuid().ToString(),
            Username = "admin",
            Email = "admin@boltwebapi.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        Console.WriteLine("Default admin user created:");
        Console.WriteLine("  Username: admin");
        Console.WriteLine("  Password: ChangeMe123!");
        Console.WriteLine("  IMPORTANT: Change this password immediately!");
    }
}
```

**Call in Program.cs:**
```csharp
// After app.Build() but before app.Run()
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BoltDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await DbInitializer.InitializeAsync(context, config);
}
```

**Requirements Met:** 5.1

---

## Implementation Checklist

### Phase 1: Core Bolt Execution Service
- [ ] Create `Models/Domain/BoltModels.cs` with all request/response models
- [ ] Create `Services/Interfaces/IBoltExecutionService.cs`
- [ ] Implement `Services/Implementations/BoltExecutionService.cs`
- [ ] Create `Validators/CommandExecutionRequestValidator.cs`
- [ ] Create `Validators/TaskExecutionRequestValidator.cs`
- [ ] Create `Validators/PlanExecutionRequestValidator.cs`
- [ ] Register service in `Program.cs`
- [ ] Add NuGet package: `Newtonsoft.Json` (if not using System.Text.Json for Bolt output parsing)

### Phase 2: Execution Endpoints
- [ ] Create `Endpoints/BoltCommandEndpoints.cs`
- [ ] Create `Endpoints/BoltTaskEndpoints.cs`
- [ ] Create `Endpoints/BoltPlanEndpoints.cs`
- [ ] Register endpoints in `Program.cs`
- [ ] Test endpoints with Swagger UI

### Phase 3: Inventory & Configuration Services
- [ ] Create `Models/Domain/InventoryModels.cs`
- [ ] Create `Services/Interfaces/IInventoryService.cs`
- [ ] Implement `Services/Implementations/InventoryService.cs`
- [ ] Create `Services/Interfaces/IConfigurationService.cs`
- [ ] Implement `Services/Implementations/ConfigurationService.cs`
- [ ] Create `Endpoints/InventoryEndpoints.cs`
- [ ] Create `Endpoints/ConfigurationEndpoints.cs`
- [ ] Register services and endpoints in `Program.cs`
- [ ] Add NuGet package: `YamlDotNet` for YAML parsing

### Phase 4: Execution History Service
- [ ] Create `Models/Domain/HistoryModels.cs`
- [ ] Create `Services/Interfaces/IExecutionHistoryService.cs`
- [ ] Implement `Services/Implementations/ExecutionHistoryService.cs`
- [ ] Create `Endpoints/HistoryEndpoints.cs`
- [ ] Register service and endpoints in `Program.cs`
- [ ] Integrate history logging in `BoltExecutionService`

### Phase 5: Real-Time Features (SignalR)
- [ ] Create `Hubs/ExecutionHub.cs` with client interface
- [ ] Map hub in `Program.cs`: `app.MapHub<ExecutionHub>("/hubs/execution")`
- [ ] Inject `IHubContext` into `BoltExecutionService`
- [ ] Add SignalR notifications to execution methods
- [ ] Test WebSocket connection with frontend client

### Phase 6: Middleware & Polish
- [ ] Create `Middleware/ExceptionHandlingMiddleware.cs`
- [ ] Create `Middleware/RateLimitingMiddleware.cs`
- [ ] Create `Data/DbInitializer.cs` for database seeding
- [ ] Register middleware in `Program.cs` (correct order)
- [ ] Call `DbInitializer` on startup
- [ ] Test error handling with invalid requests
- [ ] Test rate limiting with rapid requests

### Testing & Documentation
- [ ] Write unit tests for validators
- [ ] Write unit tests for services (with mocks)
- [ ] Write integration tests for endpoints
- [ ] Update CLAUDE.md with new endpoints and services
- [ ] Update README.md with complete API documentation
- [ ] Generate Swagger JSON schema
- [ ] Test all endpoints end-to-end

---

## Configuration Changes Required

### appsettings.json Updates

**Add to existing configuration:**
```json
{
  "Bolt": {
    "ExecutablePath": "/usr/local/bin/bolt",  // Existing
    "WorkingDirectory": "/etc/puppetlabs/bolt",  // Existing
    "DefaultTimeout": 300,  // Existing
    "MaxConcurrentExecutions": 5,  // Existing
    "InventoryFile": "inventory.yaml",  // Existing
    "ConfigFile": "bolt-project.yaml"  // Existing
  }
}
```

No changes needed - all configuration is already in place!

---

## NuGet Packages to Add

```bash
# For YAML parsing (inventory and configuration)
dotnet add package YamlDotNet --version 13.7.1

# Optional: For advanced JSON parsing if System.Text.Json is insufficient
# dotnet add package Newtonsoft.Json --version 13.0.3
```

---

## Testing Strategy

### Unit Tests

**Create test project:**
```bash
dotnet new xunit -n BoltWebAPI.Tests
cd BoltWebAPI.Tests
dotnet add reference ../BoltWebAPI.csproj
dotnet add package Moq --version 4.20.69
dotnet add package FluentValidation.TestHelper --version 11.9.0
```

**Test Coverage Targets:**
- All validators (100%)
- Service methods (80%+)
- Endpoint handlers (70%+)
- Middleware (80%+)

### Integration Tests

**Use WebApplicationFactory:**
```csharp
public class BoltCommandEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BoltCommandEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExecuteCommand_Returns_Unauthorized_Without_Token()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/bolt/commands/execute",
            new CommandExecutionRequest { Command = "test", Arguments = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

---

## Deployment Checklist

Before deploying to production:

- [ ] Change JWT secret in `appsettings.json` to strong random value
- [ ] Update Bolt executable path for target environment
- [ ] Configure database connection string (switch from SQLite to PostgreSQL/SQL Server)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Enable HTTPS with valid SSL certificate
- [ ] Configure CORS for production frontend URL
- [ ] Review and adjust rate limiting settings
- [ ] Set up log aggregation (e.g., Seq, ELK, Application Insights)
- [ ] Configure health check monitoring
- [ ] Set up automated backups for database
- [ ] Document API endpoints for consumers
- [ ] Create deployment pipeline (CI/CD)

---

## Estimated Implementation Time

| Phase | Estimated Time | Complexity |
|-------|---------------|------------|
| Phase 1: Core Bolt Execution Service | 8-12 hours | HIGH |
| Phase 2: Execution Endpoints | 4-6 hours | MEDIUM |
| Phase 3: Inventory & Configuration | 6-8 hours | MEDIUM |
| Phase 4: Execution History Service | 2-4 hours | LOW |
| Phase 5: SignalR Real-Time Features | 4-6 hours | MEDIUM |
| Phase 6: Middleware & Polish | 3-4 hours | LOW |
| Testing & Documentation | 6-8 hours | MEDIUM |
| **TOTAL** | **33-48 hours** | |

**Note:** Times are estimates for an experienced ASP.NET Core developer. Actual time may vary based on:
- Familiarity with Puppet Bolt CLI
- Testing requirements
- Error handling robustness
- Documentation thoroughness

---

## Priority Order for MVP

If you need to deliver a Minimum Viable Product (MVP) quickly, implement in this order:

1. **Phase 1** - Core Bolt Execution Service (CRITICAL)
2. **Phase 2.1** - BoltCommandEndpoints only (CRITICAL)
3. **Phase 4** - Execution History Service (HIGH)
4. **Phase 6.1** - Exception Handling Middleware (HIGH)
5. **Phase 2.2** - BoltTaskEndpoints (MEDIUM)
6. **Phase 2.3** - BoltPlanEndpoints (MEDIUM)
7. **Phase 3** - Inventory & Configuration (MEDIUM)
8. **Phase 5** - SignalR (LOW - nice to have)
9. **Phase 6.2** - Rate Limiting (LOW - add later)

This prioritization delivers command execution, history tracking, and error handling first, which provides a functional API for basic Bolt operations.

---

## Success Criteria

The implementation will be considered complete when:

1. ✅ All 8 requirements from the specification are implemented
2. ✅ All endpoint groups are functional and documented in Swagger
3. ✅ Unit tests achieve 80%+ code coverage
4. ✅ Integration tests cover all endpoints
5. ✅ Real-time output streaming works via SignalR
6. ✅ Error handling provides clear, actionable messages
7. ✅ Configuration management includes audit trail
8. ✅ Rate limiting prevents abuse
9. ✅ Database seeding creates default admin user
10. ✅ Documentation is complete (CLAUDE.md, README.md, Swagger)

---

## Contact & Support

This implementation plan was generated by Claude Code on 2025-10-23 for the Puppet Bolt Web API project.

For questions or clarifications on this plan, refer to:
- **Specification:** `.kiro/specs/puppet-bolt-web-interface/`
- **Design Document:** `.kiro/specs/puppet-bolt-web-interface/design.md`
- **Requirements:** `.kiro/specs/puppet-bolt-web-interface/requirements.md`
- **Project Guide:** `CLAUDE.md`
