using System.Security.Claims;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BoltWebAPI.Endpoints;

/// <summary>
/// Endpoints for Bolt task operations
/// </summary>
public static class BoltTaskEndpoints
{
    /// <summary>
    /// Maps all Bolt task endpoints
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bolt/tasks")
            .RequireAuthorization()
            .WithTags("Bolt Tasks");

        group.MapGet("/", ListTasks)
            .WithName("ListTasks")
            .WithSummary("List available Bolt tasks")
            .WithDescription("Retrieves a list of all available Bolt tasks from configured modules")
            .Produces<IEnumerable<BoltTask>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{taskName}", GetTaskDetails)
            .WithName("GetTaskDetails")
            .WithSummary("Get task details")
            .WithDescription("Retrieves detailed information about a specific Bolt task including parameters")
            .Produces<BoltTaskDetails>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapPost("/execute", ExecuteTask)
            .WithName("ExecuteTask")
            .WithSummary("Execute a Bolt task")
            .WithDescription("Executes a Bolt task on specified target nodes with provided parameters")
            .Produces<TaskExecutionResult>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }

    private static async Task<Results<Ok<IEnumerable<BoltTask>>, ProblemHttpResult>>
        ListTasks(
            IBoltExecutionService executionService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltTaskEndpoints");

        try
        {
            logger.LogInformation("Listing available Bolt tasks");

            var tasks = await executionService.ListTasksAsync();

            logger.LogInformation("Found {TaskCount} available tasks", tasks.Count());

            return TypedResults.Ok(tasks);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to list Bolt tasks: {Message}", ex.Message);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to List Tasks",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error listing Bolt tasks");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while listing tasks");
        }
    }

    private static async Task<Results<Ok<BoltTaskDetails>, NotFound<ErrorResponse>, ProblemHttpResult>>
        GetTaskDetails(
            string taskName,
            IBoltExecutionService executionService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltTaskEndpoints");

        try
        {
            logger.LogInformation("Retrieving details for task: {TaskName}", taskName);

            var taskDetails = await executionService.GetTaskDetailsAsync(taskName);

            logger.LogInformation("Task details retrieved: {TaskName}", taskName);

            return TypedResults.Ok(taskDetails);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Task not found: {TaskName}", taskName);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Task '{taskName}' not found",
                ErrorCode = "TASK_NOT_FOUND"
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to get task details: {TaskName}, {Message}", taskName, ex.Message);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to Get Task Details",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving task details: {TaskName}", taskName);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving task details");
        }
    }

    private static async Task<Results<Ok<TaskExecutionResult>, BadRequest<ErrorResponse>, ProblemHttpResult>>
        ExecuteTask(
            TaskExecutionRequest request,
            IValidator<TaskExecutionRequest> validator,
            IBoltExecutionService executionService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltTaskEndpoints");

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

            logger.LogWarning("Task execution request validation failed: {Errors}",
                string.Join(", ", errors.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}")));

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
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogError("User ID not found in claims");
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "User ID not found in authentication token");
            }

            logger.LogInformation(
                "Executing task: {TaskName} on {TargetCount} targets (User: {UserId})",
                request.TaskName, request.Targets.Length, userId);

            var result = await executionService.ExecuteTaskAsync(
                request.TaskName,
                request.Targets,
                request.Parameters,
                userId,
                request.TimeoutSeconds);

            logger.LogInformation(
                "Task execution completed: ExecutionId={ExecutionId}, Success={Success}, Nodes={SuccessCount}/{TotalCount}",
                result.ExecutionId, result.Success,
                result.NodeResults.Count(n => n.Success), result.NodeResults.Count);

            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid task execution request: {Message}", ex.Message);
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "INVALID_REQUEST"
            });
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Task not found: {TaskName}", request.TaskName);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Task Not Found",
                detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Task execution failed: {Message}", ex.Message);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Execution Error",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during task execution");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while executing the task");
        }
    }
}
