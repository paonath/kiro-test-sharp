# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Puppet Bolt Web API - A RESTful API backend built with ASP.NET Core 8.0 that provides a web-based interface for executing and managing Puppet Bolt commands. The API features JWT authentication, real-time command streaming via SignalR, and comprehensive execution history tracking.

## Essential Commands

### Building and Running
```bash
# Restore dependencies
dotnet restore

# Run in development mode with hot reload
dotnet watch run

# Standard run
dotnet run

# Build the project
dotnet build

# Build for production
dotnet build -c Release
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity detailed
```

### Database Operations
```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# View current database status
dotnet ef migrations list
```

## Architecture Overview

### Minimal API Pattern
This project uses **ASP.NET Core Minimal APIs** instead of traditional Controllers. All endpoints are organized in static classes within the `Endpoints/` directory.

**Key Pattern:**
- Each endpoint group has a static `MapEndpoints(IEndpointRouteBuilder app)` method
- Endpoints are registered centrally in `Program.cs` via `AuthEndpoints.MapEndpoints(app)`
- Dependencies are injected directly into endpoint handler method parameters
- Route groups provide organization: `app.MapGroup("/api/auth")`

**When adding new endpoint groups:**
1. Create a new static class in `Endpoints/` (e.g., `ExecutionEndpoints.cs`)
2. Implement `MapEndpoints(IEndpointRouteBuilder app)` method
3. Register in `Program.cs`: `ExecutionEndpoints.MapEndpoints(app)`

### Service Layer Architecture
Services follow interface-based design with clear separation:

```
Services/
├── Interfaces/              # Service contracts
│   ├── IAuthenticationService
│   └── IProcessManager
└── Implementations/         # Concrete implementations
    ├── AuthenticationService  (Scoped)
    └── ProcessManager         (Singleton)
```

**Service Responsibilities:**
- `IAuthenticationService` - JWT token generation/validation, user authentication, refresh token management, BCrypt password verification
- `IProcessManager` - External process execution (Puppet Bolt CLI), concurrent process tracking, timeout management, real-time stdout/stderr streaming

### Data Models Organization

```
Models/
├── Domain/                  # Request/Response DTOs
│   └── AuthenticationModels.cs
└── Entities/               # Database entities
    ├── UserEntity.cs
    ├── RefreshTokenEntity.cs
    ├── ExecutionHistoryEntity.cs
    └── ConfigurationChangeEntity.cs
```

**Critical Entity Relationships:**
- `UserEntity` has one-to-many with `RefreshTokenEntity` (cascade delete)
- `ExecutionHistoryEntity` tracks all command executions with audit trail
- `ConfigurationChangeEntity` provides configuration change audit history

### Database Context Configuration
`BoltDbContext` in `Data/BoltDbContext.cs` uses **Fluent API** for entity configuration:
- String enum conversions for `ExecutionType` and `ExecutionState`
- Strategic indexes on frequently queried fields (UserId, StartedAt, IsActive)
- Unique constraints on Username and Email
- Composite indexes for complex queries (e.g., `(UserId, StartedAt)`)

### Validation Pattern
All request validation uses **FluentValidation**:
- Validators are in `Validators/` directory
- Auto-registered via `AddValidatorsFromAssemblyContaining<Program>()`
- Injected directly into endpoint handlers as `IValidator<T>`
- Manual validation with structured error response grouping

**When adding new validators:**
1. Create class inheriting from `AbstractValidator<T>` in `Validators/`
2. Define rules in constructor
3. Auto-discovered by DI - no manual registration needed

## Authentication & Authorization

### JWT Authentication Flow
1. User submits credentials to `POST /api/auth/login`
2. `AuthenticationService` verifies password with BCrypt
3. Access token issued (15 min default expiration)
4. Refresh token issued (7 day default) and stored in database
5. Client uses access token in `Authorization: Bearer {token}` header
6. Use `POST /api/auth/refresh` to get new access token
7. Use `POST /api/auth/logout` to revoke refresh token

**Token Configuration:** Located in `appsettings.json` under `Authentication` section
- `JwtSecret` - **Must change in production**
- `JwtExpirationMinutes` - Access token lifetime (default: 15)
- `RefreshTokenExpirationDays` - Refresh token lifetime (default: 7)

### Adding Protected Endpoints
```csharp
app.MapGet("/api/protected-resource", () => { ... })
    .RequireAuthorization(); // Requires valid JWT
```

## Process Management with ProcessManager

`ProcessManager` service handles external CLI execution (Puppet Bolt):
- **Concurrency limiting** via semaphore (`Bolt:MaxConcurrentExecutions` in config)
- **Timeout management** with configurable defaults
- **Real-time output streaming** via callback functions
- **Process tracking** with unique execution GUIDs

**Key Methods:**
- `ExecuteAsync(executable, arguments, onStdOut, onStdErr, timeout, cancellationToken)`
- `IsProcessRunningAsync(executionId)`
- `KillProcessAsync(executionId)`

**Configuration:** In `appsettings.json` under `Bolt` section

## Configuration Structure

### appsettings.json Sections
- **Bolt** - CLI path, working directory, timeouts, concurrency limits
- **Authentication** - JWT settings, token expiration
- **Database** - Provider (SQLite/PostgreSQL/SQL Server) and connection strings
- **Logging** - Serilog configuration with file/console sinks
- **CORS** - Frontend allowed origins (default: localhost:3000, localhost:5173)

**Environment-specific overrides:** `appsettings.Development.json`

## Important Development Notes

### Enums are Stored as Strings
`ExecutionType` and `ExecutionState` enums convert to strings in the database via Fluent API configuration. This ensures readability in database queries and future-proofs enum changes.

### SignalR Infrastructure Ready
`Hubs/` directory exists with SignalR configured in `Program.cs`, but no hubs are currently implemented. Future real-time features for command output streaming will be added here.

### Password Security
- All passwords are hashed using **BCrypt.Net** before storage
- Never log or expose password hashes in API responses
- `LoginRequestValidator` enforces minimum 8-character passwords

### Database Indexes for Performance
Key indexes are configured in `BoltDbContext`:
- `UserEntity`: Username, Email (unique), IsActive
- `RefreshTokenEntity`: UserId, ExpiresAt, composite (IsRevoked, ExpiresAt)
- `ExecutionHistoryEntity`: UserId, StartedAt, State, composite (UserId, StartedAt)

### Error Response Structure
All endpoints return consistent error responses via `ErrorResponse` class:
```csharp
{
    "message": "Human-readable error",
    "errorCode": "MACHINE_READABLE_CODE",
    "validationErrors": { "PropertyName": ["Error1", "Error2"] },
    "traceId": "request-trace-id"
}
```

## API Documentation

When the application is running, access Swagger UI at:
```
https://localhost:5001/swagger
```

Swagger is configured with JWT Bearer authentication support - use the "Authorize" button to add your token.

## Project Structure Quick Reference

```
/
├── Endpoints/             # Minimal API endpoint groups
├── Services/              # Business logic (Interfaces + Implementations)
├── Models/                # Domain DTOs + Database Entities
├── Validators/            # FluentValidation request validators
├── Data/                  # DbContext and EF Core configuration
├── Hubs/                  # SignalR hubs (planned)
├── Middleware/            # Custom middleware (if needed)
├── Migrations/            # EF Core database migrations
├── Program.cs             # Application entry point and DI configuration
└── appsettings.json       # Configuration
```

## Common Development Patterns

### Adding a New Entity
1. Create entity class in `Models/Entities/`
2. Add `DbSet<YourEntity>` property to `BoltDbContext`
3. Configure in `OnModelCreating()` using Fluent API
4. Create migration: `dotnet ef migrations add AddYourEntity`
5. Apply migration: `dotnet ef database update`

### Adding a New Service
1. Create interface in `Services/Interfaces/IYourService.cs`
2. Create implementation in `Services/Implementations/YourService.cs`
3. Register in `Program.cs`: `builder.Services.AddScoped<IYourService, YourService>()`
4. Inject into endpoints or other services via constructor/parameters

### Adding a New Endpoint Group
1. Create static class in `Endpoints/YourEndpoints.cs`
2. Add `MapEndpoints(IEndpointRouteBuilder app)` method
3. Use `app.MapGroup("/api/your-route")` for organization
4. Register in `Program.cs`: `YourEndpoints.MapEndpoints(app)`

## Technology Stack

- **Framework:** ASP.NET Core 8.0 / C# 12
- **Database ORM:** Entity Framework Core 8.0.0
- **Validation:** FluentValidation 11.9.0
- **Authentication:** JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0)
- **Password Hashing:** BCrypt.Net-Next 4.0.3
- **Real-Time:** SignalR 1.1.0
- **Logging:** Serilog 8.0.0
- **API Documentation:** Swashbuckle (Swagger) 6.5.0
- **Database:** SQLite (development), configurable for PostgreSQL/SQL Server
