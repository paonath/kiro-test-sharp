using System.Security.Claims;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BoltWebAPI.Endpoints;

/// <summary>
/// Endpoints for Bolt command execution
/// </summary>
public static class BoltCommandEndpoints
{
    /// <summary>
    /// Maps all Bolt command endpoints
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bolt/commands")
            .RequireAuthorization()
            .WithTags("Bolt Commands");

        group.MapPost("/execute", ExecuteCommand)
            .WithName("ExecuteCommand")
            .WithSummary("Execute a Bolt command")
            .WithDescription("Executes an arbitrary Bolt command with specified arguments and returns the execution results")
            .Produces<CommandExecutionResult>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{executionId:guid}/status", GetExecutionStatus)
            .WithName("GetExecutionStatus")
            .WithSummary("Get execution status")
            .WithDescription("Retrieves the current status of a command execution by its unique identifier")
            .Produces<ExecutionStatus>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapPost("/{executionId:guid}/cancel", CancelExecution)
            .WithName("CancelExecution")
            .WithSummary("Cancel a running execution")
            .WithDescription("Cancels a running command execution by its unique identifier")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<Results<Ok<CommandExecutionResult>, BadRequest<ErrorResponse>, ProblemHttpResult>>
        ExecuteCommand(
            CommandExecutionRequest request,
            IValidator<CommandExecutionRequest> validator,
            IBoltExecutionService executionService,
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

            logger.LogWarning("Command execution request validation failed: {Errors}",
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
                "Executing command: {Command} with {ArgumentCount} arguments (User: {UserId})",
                request.Command, request.Arguments?.Length ?? 0, userId);

            var result = await executionService.ExecuteCommandAsync(
                request.Command,
                request.Arguments ?? Array.Empty<string>(),
                userId,
                request.TimeoutSeconds);

            logger.LogInformation(
                "Command execution completed: ExecutionId={ExecutionId}, ExitCode={ExitCode}, Success={Success}",
                result.ExecutionId, result.ExitCode, result.Success);

            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid command execution request: {Message}", ex.Message);
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "INVALID_REQUEST"
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Command execution failed: {Message}", ex.Message);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Execution Error",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during command execution");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while executing the command");
        }
    }

    private static async Task<Results<Ok<ExecutionStatus>, NotFound<ErrorResponse>>>
        GetExecutionStatus(
            Guid executionId,
            IBoltExecutionService executionService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltCommandEndpoints");

        try
        {
            logger.LogInformation("Retrieving execution status for: {ExecutionId}", executionId);

            var status = await executionService.GetExecutionStatusAsync(executionId);

            logger.LogInformation(
                "Execution status retrieved: ExecutionId={ExecutionId}, State={State}",
                executionId, status.State);

            return TypedResults.Ok(status);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Execution not found: {ExecutionId}", executionId);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Execution with ID {executionId} not found",
                ErrorCode = "EXECUTION_NOT_FOUND"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving execution status: {ExecutionId}", executionId);
            throw;
        }
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>, ProblemHttpResult>>
        CancelExecution(
            Guid executionId,
            IBoltExecutionService executionService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltCommandEndpoints");

        try
        {
            logger.LogInformation("Cancelling execution: {ExecutionId}", executionId);

            await executionService.CancelExecutionAsync(executionId);

            logger.LogInformation("Execution cancelled successfully: {ExecutionId}", executionId);

            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Execution not found for cancellation: {ExecutionId}", executionId);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Execution with ID {executionId} not found or already completed",
                ErrorCode = "EXECUTION_NOT_FOUND"
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot cancel execution: {ExecutionId}, Reason: {Message}",
                executionId, ex.Message);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Cannot Cancel Execution",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling execution: {ExecutionId}", executionId);
            throw;
        }
    }
}
