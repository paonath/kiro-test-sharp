using System.Security.Claims;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BoltWebAPI.Endpoints;

/// <summary>
/// Endpoints for Bolt configuration management
/// </summary>
public static class ConfigurationEndpoints
{
    /// <summary>
    /// Maps all configuration endpoints
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/configuration")
            .RequireAuthorization()
            .WithTags("Configuration");

        // Get operations
        group.MapGet("/", GetConfiguration)
            .WithName("GetConfiguration")
            .WithSummary("Get Bolt configuration")
            .WithDescription("Retrieves the complete Bolt configuration")
            .Produces<BoltConfiguration>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/transport/{transport}", GetTransportConfig)
            .WithName("GetTransportConfig")
            .WithSummary("Get transport configuration")
            .WithDescription("Retrieves configuration for a specific transport (ssh, winrm, etc.)")
            .Produces<TransportConfig>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/modulepath", GetModulepath)
            .WithName("GetModulepath")
            .WithSummary("Get modulepath")
            .WithDescription("Retrieves the configured Bolt modulepath")
            .Produces<IEnumerable<string>>(StatusCodes.Status200OK);

        // Update operations
        group.MapPut("/", UpdateConfiguration)
            .WithName("UpdateConfiguration")
            .WithSummary("Update Bolt configuration")
            .WithDescription("Updates the entire Bolt configuration from YAML content")
            .Produces<BoltConfiguration>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPut("/transport", UpdateTransportConfig)
            .WithName("UpdateTransportConfig")
            .WithSummary("Update transport configuration")
            .WithDescription("Updates configuration for a specific transport")
            .Produces<TransportConfig>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPut("/modulepath", UpdateModulepath)
            .WithName("UpdateModulepath")
            .WithSummary("Update modulepath")
            .WithDescription("Updates the Bolt modulepath configuration")
            .Produces<IEnumerable<string>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Validation
        group.MapPost("/validate", ValidateConfiguration)
            .WithName("ValidateConfiguration")
            .WithSummary("Validate configuration YAML")
            .WithDescription("Validates Bolt configuration YAML content without saving")
            .Produces<ConfigurationValidationResult>(StatusCodes.Status200OK);
    }

    private static async Task<Results<Ok<BoltConfiguration>, NotFound<ErrorResponse>, ProblemHttpResult>>
        GetConfiguration(
            IConfigurationService configurationService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConfigurationEndpoints");

        try
        {
            var config = await configurationService.GetConfigurationAsync();
            return TypedResults.Ok(config);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogWarning("Configuration file not found: {Message}", ex.Message);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = "Configuration file not found",
                ErrorCode = "CONFIG_NOT_FOUND"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving configuration");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error Retrieving Configuration",
                detail: ex.Message);
        }
    }

    private static async Task<Results<Ok<TransportConfig>, NotFound<ErrorResponse>>>
        GetTransportConfig(
            string transport,
            IConfigurationService configurationService,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConfigurationEndpoints");

        try
        {
            var transportConfig = await configurationService.GetTransportConfigAsync(transport);
            return TypedResults.Ok(transportConfig);
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Transport not configured: {Transport}", transport);
            return TypedResults.NotFound(new ErrorResponse
            {
                Message = $"Transport '{transport}' not configured",
                ErrorCode = "TRANSPORT_NOT_FOUND"
            });
        }
    }

    private static async Task<Ok<IEnumerable<string>>>
        GetModulepath(IConfigurationService configurationService)
    {
        var modulepath = await configurationService.GetModulepathAsync();
        return TypedResults.Ok(modulepath);
    }

    private static async Task<Results<Ok<BoltConfiguration>, BadRequest<ErrorResponse>, ProblemHttpResult>>
        UpdateConfiguration(
            UpdateConfigurationRequest request,
            IValidator<UpdateConfigurationRequest> validator,
            IConfigurationService configurationService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConfigurationEndpoints");

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
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var config = await configurationService.UpdateConfigurationAsync(request, userId);

            logger.LogInformation("Updated configuration (User: {UserId})", userId);

            return TypedResults.Ok(config);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid configuration update request");
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "INVALID_REQUEST"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating configuration");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error Updating Configuration",
                detail: ex.Message);
        }
    }

    private static async Task<Results<Ok<TransportConfig>, BadRequest<ErrorResponse>>>
        UpdateTransportConfig(
            UpdateTransportRequest request,
            IValidator<UpdateTransportRequest> validator,
            IConfigurationService configurationService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConfigurationEndpoints");

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
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var transportConfig = await configurationService.UpdateTransportConfigAsync(request, userId);

            logger.LogInformation("Updated transport {Transport} (User: {UserId})", request.Transport, userId);

            return TypedResults.Ok(transportConfig);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid transport update request");
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "INVALID_REQUEST"
            });
        }
    }

    private static async Task<Results<Ok<IEnumerable<string>>, BadRequest<ErrorResponse>>>
        UpdateModulepath(
            List<string> modulepath,
            IConfigurationService configurationService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ConfigurationEndpoints");

        if (modulepath == null || !modulepath.Any())
        {
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = "Modulepath cannot be empty",
                ErrorCode = "INVALID_REQUEST"
            });
        }

        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var updatedModulepath = await configurationService.UpdateModulepathAsync(modulepath, userId);

            logger.LogInformation("Updated modulepath (User: {UserId})", userId);

            return TypedResults.Ok(updatedModulepath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating modulepath");
            return TypedResults.BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                ErrorCode = "UPDATE_ERROR"
            });
        }
    }

    private static async Task<Ok<ConfigurationValidationResult>>
        ValidateConfiguration(
            UpdateConfigurationRequest request,
            IConfigurationService configurationService)
    {
        var result = await configurationService.ValidateConfigurationAsync(request.YamlContent);
        return TypedResults.Ok(result);
    }
}
