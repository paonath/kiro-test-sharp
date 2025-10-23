using BoltWebAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BoltWebAPI.Data;

/// <summary>
/// Database seeder for initial data
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds the database with initial data if empty
    /// </summary>
    public static async Task SeedAsync(BoltDbContext context, IConfiguration configuration, ILogger logger)
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if we need to seed
            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Database already contains users, skipping seeding");
                return;
            }

            logger.LogInformation("Seeding database with initial data...");

            // Create default admin user
            var defaultUsername = configuration["DefaultUser:Username"] ?? "admin";
            var defaultPassword = configuration["DefaultUser:Password"] ?? "Admin123!";
            var defaultRole = configuration["DefaultUser:Role"] ?? "Admin";

            var adminUser = new UserEntity
            {
                Id = Guid.NewGuid().ToString(),
                Username = defaultUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                Role = defaultRole,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            logger.LogInformation(
                "Default admin user created: Username={Username}, Role={Role}",
                defaultUsername, defaultRole);

            logger.LogWarning(
                "IMPORTANT: Default admin password is '{Password}'. Please change it immediately after first login!",
                defaultPassword);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    /// <summary>
    /// Extension method to seed database during application startup
    /// </summary>
    public static async Task<IApplicationBuilder> SeedDatabaseAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BoltDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DatabaseSeeder");

        await SeedAsync(context, configuration, logger);

        return app;
    }
}
