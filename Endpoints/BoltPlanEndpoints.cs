using System.Security.Claims;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BoltWebAPI.Endpoints;

/// <summary>
/// Endpoints for Bolt plan operations
/// </summary>
public static class BoltPlanEndpoints
{
    /// <summary>
    /// Maps all Bolt plan endpoints
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bolt/plans")
            .RequireAuthorization()
            .WithTags("Bolt Plans");

        group.MapGet("/", ListPlans)
            .WithName("ListPlans")
            .WithSummary("List available Bolt plans")
            .WithDescription("Retrieves a list of all available Bolt plans from configured modules")
            .Produces<IEnumerable<BoltPlan>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{planName}", GetPlanDetails)
            .WithName("GetPlanDetails")
            .WithSummary("Get plan details")
            .WithDescription("Retrieves detailed information about a specific Bolt plan including parameters")
            .Produces<BoltPlanDetails>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapPost("/execute", ExecutePlan)
            .WithName("ExecutePlan")
            .WithSummary("Execute a Bolt plan")
            .WithDescription("Executes a Bolt plan with provided parameters")
            .Produces<PlanExecutionResult>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }

    private static async Task<Results<Ok<IEnumerable<BoltPlan>>, ProblemHttpResult>>
        ListPlans(
            IBoltExecutionService executionService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltPlanEndpoints");

        try
        {
            logger.LogInformation("Listing available Bolt plans");

            var plans = await executionService.ListPlansAsync();

            logger.LogInformation("Found {PlanCount} available plans", plans.Count());

            return TypedResults.Ok(plans);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to list Bolt plans: {Message}", ex.Message);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to List Plans",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error listing Bolt plans");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while listing plans");
        }
    }

    private static async Task<Results<Ok<BoltPlanDetails>, NotFound<ErrorResponse>, ProblemHttpResult>>
        GetPlanDetails(
            string planName,
            IBoltExecutionService executionService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltPlanEndpoints");

        try
        {
            logger.LogInformation("Retrieving details for plan: {PlanName}", planName);

            var planDetails = await executionService.GetPlanDetailsAsync(planName);

            logger.LogInformation("Plan details retrieved: {PlanName}", planName);

            return TypedResults.Ok(planDetails);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Plan not found: {PlanName}", planName);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Plan '{planName}' not found",
                ErrorCode = "PLAN_NOT_FOUND"
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to get plan details: {PlanName}, {Message}", planName, ex.Message);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to Get Plan Details",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving plan details: {PlanName}", planName);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while retrieving plan details");
        }
    }

    private static async Task<Results<Ok<PlanExecutionResult>, BadRequest<ErrorResponse>, ProblemHttpResult>>
        ExecutePlan(
            PlanExecutionRequest request,
            IValidator<PlanExecutionRequest> validator,
            IBoltExecutionService executionService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BoltPlanEndpoints");

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

            logger.LogWarning("Plan execution request validation failed: {Errors}",
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
                "Executing plan: {PlanName} (User: {UserId})",
                request.PlanName, userId);

            var result = await executionService.ExecutePlanAsync(
                request.PlanName,
                request.Parameters,
                userId,
                request.TimeoutSeconds);

            logger.LogInformation(
                "Plan execution completed: ExecutionId={ExecutionId}, Success={Success}",
                result.ExecutionId, result.Success);

            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid plan execution request: {Message}", ex.Message);
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "INVALID_REQUEST"
            });
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Plan not found: {PlanName}", request.PlanName);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Plan Not Found",
                detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Plan execution failed: {Message}", ex.Message);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Execution Error",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during plan execution");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred while executing the plan");
        }
    }
}
