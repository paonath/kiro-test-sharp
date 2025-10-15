# Migration to Minimal APIs with FluentValidation

## Overview

The Puppet Bolt Web Interface design has been updated to use **Minimal APIs** instead of traditional Controllers, and **FluentValidation** for request validation instead of data annotations.

## Key Changes

### 1. Architecture Updates

**Before:**
- Controllers in `Controllers/` folder
- Data annotations for validation
- `[ApiController]` and `[Route]` attributes
- `ActionResult<T>` return types

**After:**
- Endpoint definitions in `Endpoints/` folder
- FluentValidation validators in `Validators/` folder
- Route groups with `.MapGroup()` and `.MapPost()/.MapGet()` etc.
- `Results<T>` return types for type-safe responses

### 2. Folder Structure Changes

```
Old Structure:                  New Structure:
├── Controllers/                ├── Endpoints/
│   ├── AuthController.cs       │   ├── AuthEndpoints.cs
│   ├── BoltCommandController   │   ├── BoltCommandEndpoints.cs
│   └── ...                     │   └── ...
                                ├── Validators/
                                │   ├── LoginRequestValidator.cs
                                │   ├── CommandExecutionRequestValidator.cs
                                │   └── ...
```

### 3. Endpoint Definition Pattern

**Before (Controller):**
```csharp
[ApiController]
[Route("api/bolt/commands")]
[Authorize]
public class BoltCommandController : ControllerBase
{
    [HttpPost("execute")]
    public async Task<ActionResult<CommandExecutionResult>> ExecuteCommand(
        [FromBody] CommandExecutionRequest request)
    {
        // Implementation
    }
}
```

**After (Minimal API):**
```csharp
public static class BoltCommandEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bolt/commands")
            .RequireAuthorization()
            .WithTags("Bolt Commands");
        
        group.MapPost("/execute", ExecuteCommand);
    }
    
    private static async Task<Results<Ok<CommandExecutionResult>, BadRequest<ErrorResponse>>> 
        ExecuteCommand(
            CommandExecutionRequest request,
            IValidator<CommandExecutionRequest> validator,
            IBoltExecutionService service,
            ClaimsPrincipal user)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.BadRequest(new ErrorResponse 
            { 
                ValidationErrors = validationResult.ToDictionary() 
            });
        }
        
        var result = await service.ExecuteCommandAsync(
            request.Command, 
            request.Arguments, 
            user.Identity.Name);
        
        return TypedResults.Ok(result);
    }
}
```

### 4. Validation Pattern

**Before (Data Annotations):**
```csharp
public class CommandExecutionRequest
{
    [Required]
    [MaxLength(500)]
    public string Command { get; set; }
    
    [Required]
    public string[] Arguments { get; set; }
}
```

**After (FluentValidation):**
```csharp
// Model (clean, no attributes)
public class CommandExecutionRequest
{
    public string Command { get; set; }
    public string[] Arguments { get; set; }
    public int? TimeoutSeconds { get; set; }
}

// Validator
public class CommandExecutionRequestValidator : AbstractValidator<CommandExecutionRequest>
{
    public CommandExecutionRequestValidator()
    {
        RuleFor(x => x.Command)
            .NotEmpty()
            .MaximumLength(500);
        
        RuleFor(x => x.Arguments)
            .NotNull();
        
        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(3600)
            .When(x => x.TimeoutSeconds.HasValue);
    }
}
```

### 5. Authorization Pattern

**Before:**
```csharp
[Authorize(Roles = "Admin")]
public class ConfigurationController : ControllerBase
{
    // ...
}
```

**After:**
```csharp
var group = app.MapGroup("/api/bolt/config")
    .RequireAuthorization(policy => policy.RequireRole("Admin"))
    .WithTags("Configuration");
```

### 6. Program.cs Registration

**New registrations needed:**
```csharp
// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Map all endpoint groups
app.MapAuthEndpoints();
app.MapBoltCommandEndpoints();
app.MapBoltTaskEndpoints();
app.MapBoltPlanEndpoints();
app.MapInventoryEndpoints();
app.MapConfigurationEndpoints();
app.MapHistoryEndpoints();
```

## Benefits of This Approach

### Minimal APIs
1. **Performance**: Reduced overhead compared to controllers
2. **Simplicity**: Less boilerplate code
3. **Modern**: Aligns with .NET 8 best practices
4. **Type Safety**: `Results<T>` provides compile-time type checking
5. **Testability**: Easier to test individual endpoint handlers

### FluentValidation
1. **Separation of Concerns**: Validation logic separate from models
2. **Reusability**: Validators can be composed and reused
3. **Testability**: Easy to unit test validation rules
4. **Expressiveness**: More readable and maintainable validation rules
5. **Complex Validation**: Better support for conditional and cross-property validation
6. **Custom Messages**: More control over error messages

## Migration Checklist

- [x] Update steering files (structure.md, tech.md)
- [x] Update design.md with Minimal API patterns
- [x] Update tasks.md to reflect endpoint implementation
- [x] Add FluentValidation validators to design
- [ ] Implement endpoint classes
- [ ] Implement FluentValidation validators
- [ ] Update Program.cs with registrations
- [ ] Update Swagger configuration for Minimal APIs
- [ ] Update integration tests for new endpoint structure

## Example Validators to Implement

1. `LoginRequestValidator` - Validates username and password
2. `CommandExecutionRequestValidator` - Validates Bolt commands
3. `TaskExecutionRequestValidator` - Validates task execution requests
4. `PlanExecutionRequestValidator` - Validates plan execution requests
5. `BoltConfigurationValidator` - Validates configuration updates
6. `RefreshTokenRequestValidator` - Validates token refresh requests

## Testing Considerations

### Unit Testing Validators
```csharp
public class CommandExecutionRequestValidatorTests
{
    private readonly CommandExecutionRequestValidator _validator = new();
    
    [Fact]
    public void Should_Have_Error_When_Command_Is_Empty()
    {
        var request = new CommandExecutionRequest { Command = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Command);
    }
}
```

### Integration Testing Endpoints
```csharp
public class BoltCommandEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task ExecuteCommand_Returns_BadRequest_When_Invalid()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/bolt/commands/execute",
            new { Command = "" });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

## References

- [Minimal APIs in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [ASP.NET Core 8 Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
