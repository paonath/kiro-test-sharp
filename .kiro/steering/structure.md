# Project Structure

## Architecture Pattern

The project follows a layered architecture with clear separation of concerns:

1. **Presentation Layer**: Minimal API Endpoints, SignalR Hubs
2. **Business Logic Layer**: Services (interfaces and implementations)
3. **Data Access Layer**: Repositories, Entity Framework DbContext
4. **Infrastructure Layer**: Process management, file system access, CLI interaction

## Folder Organization

```
/
├── .kiro/                  # Kiro IDE configuration and documentation
│   ├── docs/              # General Kiro documentation (guides, migration docs, etc.)
│   ├── specs/             # Feature specifications and design docs
│   ├── steering/          # Project steering rules and conventions
│   └── settings/          # Kiro settings (MCP, etc.)
├── Endpoints/             # Minimal API endpoint definitions
│   ├── AuthEndpoints.cs
│   ├── BoltCommandEndpoints.cs
│   ├── BoltTaskEndpoints.cs
│   ├── BoltPlanEndpoints.cs
│   ├── InventoryEndpoints.cs
│   ├── ConfigurationEndpoints.cs
│   └── HistoryEndpoints.cs
├── Services/              # Business logic
│   ├── Interfaces/
│   │   ├── IBoltExecutionService.cs
│   │   ├── IProcessManager.cs
│   │   ├── IInventoryService.cs
│   │   ├── IConfigurationService.cs
│   │   ├── IExecutionHistoryService.cs
│   │   └── IAuthenticationService.cs
│   └── Implementations/
├── Models/                # Data models
│   ├── Domain/           # Request/response models
│   └── Entities/         # Database entities
├── Validators/           # FluentValidation validators
│   ├── CommandExecutionRequestValidator.cs
│   ├── TaskExecutionRequestValidator.cs
│   ├── PlanExecutionRequestValidator.cs
│   └── LoginRequestValidator.cs
├── Data/                 # Database context and migrations
│   ├── BoltDbContext.cs
│   └── Migrations/
├── Hubs/                 # SignalR hubs
│   └── ExecutionHub.cs
├── Middleware/           # Custom middleware
│   └── ExceptionHandlingMiddleware.cs
├── appsettings.json      # Configuration
└── Program.cs            # Application entry point
```

## Kiro Documentation Convention

**IMPORTANT**: All Kiro-related documentation and configuration files MUST be placed in the `.kiro/` subdirectory:

- **Docs**: `.kiro/docs/` - General Kiro documentation, guides, migration documents, architecture decisions
- **Specs**: `.kiro/specs/` - Feature specifications, design documents, requirements, and implementation tasks
- **Steering**: `.kiro/steering/` - Project conventions, technology stack, architecture patterns
- **Settings**: `.kiro/settings/` - Kiro IDE settings like MCP configuration
- **Hooks**: `.kiro/hooks/` - Agent hook definitions (if used)

This keeps Kiro documentation separate from application code and makes it easy to:
- Exclude from production builds
- Version control separately if needed
- Maintain a clean project root
- Distinguish between application docs (README, API docs) and development workflow docs

## Key Conventions

### Minimal API Endpoints
- Endpoints are defined in static classes in the `Endpoints/` folder
- Each endpoint class has a `MapEndpoints(IEndpointRouteBuilder app)` method
- Route pattern: `/api/{resource}` (e.g., `/api/bolt/commands`)
- Protected endpoints use `.RequireAuthorization()`
- Admin-only endpoints use `.RequireAuthorization(policy => policy.RequireRole("Admin"))`
- All endpoints return `Results<T>` for type-safe responses

### Services
- All services have an interface in `Services/Interfaces/`
- Implementations go in `Services/Implementations/`
- Services are registered in `Program.cs` with appropriate lifetime (Scoped/Singleton/Transient)
- Async methods use `Async` suffix and return `Task<T>`

### Models
- Domain models (DTOs) in `Models/Domain/` for API contracts
- Entity models in `Models/Entities/` for database mapping
- Keep entities separate from DTOs to avoid tight coupling
- Request models are validated using FluentValidation

### Validators
- All validators inherit from `AbstractValidator<T>` from FluentValidation
- Validators are in the `Validators/` folder
- Validators are registered automatically via `AddValidatorsFromAssemblyContaining<T>()`
- Validation is performed using `IValidator<T>.ValidateAsync()` in endpoints
- Return 400 Bad Request with validation errors if validation fails

### Error Handling
- Global exception handling via middleware
- Return structured `ErrorResponse` model for all errors
- Use appropriate HTTP status codes (400, 401, 403, 404, 408, 500)
- Log all exceptions with trace IDs

### Naming Conventions
- PascalCase for classes, methods, properties, public fields
- camelCase for local variables and parameters
- Prefix interfaces with `I`
- Suffix async methods with `Async`
- Use descriptive names that indicate purpose

### Security
- Never log sensitive data (passwords, tokens, keys)
- Validate and sanitize all user inputs
- Use parameterized queries (EF Core handles this)
- Implement rate limiting on all endpoints
- HTTPS only in production
