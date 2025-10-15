# Setup Guide

## Project Setup Complete

Task 1 has been completed. The project structure and core configuration are now in place.

## What Was Created

### Project Files
- **BoltWebAPI.csproj**: .NET 8.0 Web API project with all required NuGet packages
- **Program.cs**: Application entry point with JWT authentication, SignalR, CORS, and Swagger configured
- **appsettings.json**: Main configuration file with Bolt, Authentication, Database, and Logging settings
- **appsettings.Development.json**: Development-specific configuration overrides
- **global.json**: Specifies .NET SDK version 8.0
- **README.md**: Project documentation
- **.gitignore**: Git ignore rules for .NET projects

### Folder Structure
- **Endpoints/**: Minimal API endpoint definitions (ready for implementation)
- **Services/Interfaces/**: Service interface definitions
- **Services/Implementations/**: Service implementations
- **Models/Domain/**: Request/response DTOs
- **Models/Entities/**: Database entity models
- **Validators/**: FluentValidation validators
- **Data/**: Database context and migrations
- **Hubs/**: SignalR hubs for real-time communication
- **Middleware/**: Custom middleware
- **Properties/**: Launch settings for development

### NuGet Packages Included
- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.Sqlite (8.0.0)
- Microsoft.EntityFrameworkCore.Design (8.0.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
- FluentValidation (11.9.0)
- FluentValidation.DependencyInjectionExtensions (11.9.0)
- Serilog.AspNetCore (8.0.0)
- Serilog.Sinks.File (5.0.0)
- Serilog.Sinks.Console (5.0.1)
- Microsoft.AspNetCore.SignalR (1.1.0)
- Swashbuckle.AspNetCore (6.5.0)
- BCrypt.Net-Next (4.0.3)

## Next Steps

To continue with the implementation:

1. **Install .NET 8.0 SDK** if not already installed:
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0

2. **Restore packages**:
   ```bash
   dotnet restore
   ```

3. **Verify the build**:
   ```bash
   dotnet build
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```
   Or with hot reload:
   ```bash
   dotnet watch run
   ```

5. **Access Swagger UI**:
   - Navigate to: https://localhost:5001/swagger

6. **Proceed to Task 2**: Implement database layer and entities

## Configuration Notes

### Important: Change JWT Secret
Before deploying to production, you MUST change the JWT secret in `appsettings.json`:
```json
"Authentication": {
  "JwtSecret": "your-secret-key-change-this-in-production"
}
```

Generate a strong secret (at least 32 characters) and store it securely (environment variable or secret manager).

### Bolt Configuration
Update the Bolt executable path in `appsettings.json` to match your system:
```json
"Bolt": {
  "ExecutablePath": "/usr/local/bin/bolt",
  "WorkingDirectory": "/etc/puppetlabs/bolt"
}
```

### Database
The default configuration uses SQLite for development. For production, update the connection string and provider in `appsettings.json`.

## Troubleshooting

### .NET SDK Not Found
If you get "command not found: dotnet", install the .NET 8.0 SDK from Microsoft's website.

### Package Restore Issues
If package restore fails, try:
```bash
dotnet nuget locals all --clear
dotnet restore
```

### Port Already in Use
If ports 5000/5001 are in use, update `Properties/launchSettings.json` to use different ports.
