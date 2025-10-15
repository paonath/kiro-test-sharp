# Project Structure

## Architecture Pattern

The project follows a layered architecture with clear separation of concerns:

1. **Presentation Layer**: API Controllers, SignalR Hubs
2. **Business Logic Layer**: Services (interfaces and implementations)
3. **Data Access Layer**: Repositories, Entity Framework DbContext
4. **Infrastructure Layer**: Process management, file system access, CLI interaction

## Folder Organization

```
/
├── Controllers/              # API endpoints
│   ├── AuthController.cs
│   ├── BoltCommandController.cs
│   ├── BoltTaskController.cs
│   ├── BoltPlanController.cs
│   ├── InventoryController.cs
│   ├── ConfigurationController.cs
│   └── HistoryController.cs
├── Services/                 # Business logic
│   ├── Interfaces/
│   │   ├── IBoltExecutionService.cs
│   │   ├── IProcessManager.cs
│   │   ├── IInventoryService.cs
│   │   ├── IConfigurationService.cs
│   │   ├── IExecutionHistoryService.cs
│   │   └── IAuthenticationService.cs
│   └── Implementations/
├── Models/                   # Data models
│   ├── Domain/              # Request/response models
│   └── Entities/            # Database entities
├── Data/                    # Database context and migrations
│   ├── BoltDbContext.cs
│   └── Migrations/
├── Hubs/                    # SignalR hubs
│   └── ExecutionHub.cs
├── Middleware/              # Custom middleware
│   └── ExceptionHandlingMiddleware.cs
├── appsettings.json         # Configuration
└── Program.cs               # Application entry point
```

## Key Conventions

### Controllers
- All controllers inherit from `ControllerBase`
- Use `[ApiController]` attribute for automatic model validation
- Route pattern: `[Route("api/[controller]")]` or explicit routes
- Protected endpoints use `[Authorize]` attribute
- Admin-only endpoints use `[Authorize(Roles = "Admin")]`

### Services
- All services have an interface in `Services/Interfaces/`
- Implementations go in `Services/Implementations/`
- Services are registered in `Program.cs` with appropriate lifetime (Scoped/Singleton/Transient)
- Async methods use `Async` suffix and return `Task<T>`

### Models
- Domain models (DTOs) in `Models/Domain/` for API contracts
- Entity models in `Models/Entities/` for database mapping
- Use data annotations for validation on request models
- Keep entities separate from DTOs to avoid tight coupling

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
