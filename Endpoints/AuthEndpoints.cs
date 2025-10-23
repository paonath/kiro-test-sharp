using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using BoltWebAPI.Models.Domain;
using BoltWebAPI.Services.Interfaces;

namespace BoltWebAPI.Endpoints;

public static class AuthEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/login", Login)
            .WithName("Login")
            .RequireRateLimiting("AuthenticationPolicy")
            .Produces<AuthenticationResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshToken)
            .WithName("RefreshToken")
            .RequireRateLimiting("AuthenticationPolicy")
            .Produces<AuthenticationResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", Logout)
            .RequireAuthorization()
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        IAuthenticationService authService,
        IValidator<LoginRequest> validator,
        ILoggerFactory loggerFactory)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return Results.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors
            });
        }

        var logger = loggerFactory.CreateLogger("AuthEndpoints");
        
        try
        {
            var response = await authService.AuthenticateAsync(request.Username, request.Password);
            logger.LogInformation("User {Username} logged in successfully", request.Username);
            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Login failed for username: {Username}", request.Username);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return Results.Problem(
                title: "An error occurred during login",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        IAuthenticationService authService,
        IValidator<RefreshTokenRequest> validator,
        ILoggerFactory loggerFactory)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return Results.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors
            });
        }

        var logger = loggerFactory.CreateLogger("AuthEndpoints");
        
        try
        {
            var response = await authService.RefreshTokenAsync(request.RefreshToken);
            logger.LogInformation("Token refreshed successfully");
            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Token refresh failed");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token refresh");
            return Results.Problem(
                title: "An error occurred during token refresh",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> Logout(
        [FromBody] RefreshTokenRequest request,
        IAuthenticationService authService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("AuthEndpoints");
        
        try
        {
            await authService.RevokeTokenAsync(request.RefreshToken);
            logger.LogInformation("User logged out successfully");
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout");
            return Results.Problem(
                title: "An error occurred during logout",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
