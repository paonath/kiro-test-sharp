# Puppet Bolt Web API

A RESTful API backend built with ASP.NET Core 8.0 that provides a web-based interface for executing and managing Puppet Bolt commands.

## Features

- Execute Bolt commands, tasks, and plans remotely
- Real-time streaming of command output via SignalR WebSocket
- Manage Bolt inventory and configuration through REST API
- Track execution history with detailed logging
- JWT-based authentication with role-based access control
- Multi-user support with audit trails

## Prerequisites

- .NET 8.0 SDK
- Puppet Bolt CLI installed and accessible
- SQLite (for development) or PostgreSQL/SQL Server (for production)

## Getting Started

### Installation

1. Clone the repository
2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Update `appsettings.json` with your configuration:
   - Set the correct Bolt executable path
   - Configure JWT secret (change from default)
   - Set database connection string if not using SQLite

### Running the Application

Development mode with hot reload:
```bash
dotnet watch run
```

Standard run:
```bash
dotnet run
```

The API will be available at `https://localhost:5001` (or the port specified in launchSettings.json).

### API Documentation

Once running, access the Swagger UI at:
```
https://localhost:5001/swagger
```

## Project Structure

```
/
├── Endpoints/             # Minimal API endpoint definitions
├── Services/              # Business logic
│   ├── Interfaces/
│   └── Implementations/
├── Models/                # Data models
│   ├── Domain/           # Request/response models
│   └── Entities/         # Database entities
├── Validators/           # FluentValidation validators
├── Data/                 # Database context and migrations
├── Hubs/                 # SignalR hubs
├── Middleware/           # Custom middleware
├── appsettings.json      # Configuration
└── Program.cs            # Application entry point
```

## Configuration

Key configuration sections in `appsettings.json`:

- **Bolt**: CLI path, working directory, timeouts, concurrency limits
- **Authentication**: JWT settings, token expiration
- **Database**: Provider and connection strings
- **Logging**: Log levels and file paths
- **RateLimiting**: API rate limit settings

## Security

- HTTPS enforced in production
- JWT authentication required for all protected endpoints
- Role-based authorization (Admin, User)
- Input validation using FluentValidation
- Command injection prevention
- Rate limiting enabled

## Development

### Running Tests

```bash
dotnet test
```

### Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

## License

[Your License Here]
