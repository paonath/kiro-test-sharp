# Technology Stack

## Framework & Language

- **Framework**: ASP.NET Core 8.0 (LTS)
- **Language**: C# 12
- **Runtime**: .NET 8.0

## Key Libraries & Dependencies

- **ORM**: Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens)
- **Validation**: FluentValidation for request validation
- **Real-time Communication**: SignalR for WebSocket streaming
- **Logging**: Serilog with structured logging
- **API Documentation**: Swagger/OpenAPI (with Swashbuckle for Minimal APIs)
- **Testing**: xUnit, Moq
- **Process Management**: System.Diagnostics.Process

## Database

- **Development**: SQLite
- **Production**: PostgreSQL or SQL Server

## External Dependencies

- **Puppet Bolt CLI**: Must be installed and accessible on the system

## Common Commands

Since this is a new project, these commands will be relevant once implemented:

### Development
```bash
dotnet run                          # Run the application
dotnet watch run                    # Run with hot reload
dotnet build                        # Build the project
```

### Testing
```bash
dotnet test                         # Run all tests
dotnet test --logger "console;verbosity=detailed"  # Verbose test output
```

### Database
```bash
dotnet ef migrations add <name>     # Create new migration
dotnet ef database update           # Apply migrations
```

### Docker
```bash
docker build -t bolt-web-api .      # Build container
docker-compose up                   # Run with dependencies
```

## Configuration

Application settings are managed through `appsettings.json` with environment-specific overrides (`appsettings.Development.json`, `appsettings.Production.json`).

Key configuration sections:
- Bolt: CLI path, working directory, timeouts
- Authentication: JWT settings, token expiration
- Database: Provider and connection strings
- Logging: Log levels and file paths
- RateLimiting: API rate limit settings
