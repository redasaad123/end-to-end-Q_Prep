using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ProjectAPI.Extensions
{
    public static class DatabaseExtensions
    {
        public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();
            var context = services.GetRequiredService<AppDbContext>();

            try
            {
                logger.LogInformation("🔍 Checking database connectivity...");
                
                // Test basic connectivity
                bool canConnect = false;
                
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        canConnect = await context.Database.CanConnectAsync();
                        if (canConnect)
                        {
                            logger.LogInformation("✅ Database connection successful on attempt {Attempt}", attempt);
                            break;
                        }
                        
                        logger.LogWarning("⚠️ Database connection failed on attempt {Attempt}, retrying...", attempt);
                        await Task.Delay(2000 * attempt); // Exponential backoff
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("⚠️ Database connection attempt {Attempt} failed: {Message}", attempt, ex.Message);
                        if (attempt == 3)
                        {
                            throw; // Re-throw on final attempt
                        }
                        await Task.Delay(2000 * attempt);
                    }
                }

                if (canConnect)
                {
                    // Check for pending migrations
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                    
                    logger.LogInformation("📊 Applied migrations: {Count}", appliedMigrations.Count());
                    
                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation("🔄 Found {Count} pending migrations", pendingMigrations.Count());
                        logger.LogInformation("   Migrations: {Migrations}", string.Join(", ", pendingMigrations));
                        
                        if (app.Environment.IsDevelopment())
                        {
                            logger.LogInformation("🚀 Applying migrations in development environment...");
                            await context.Database.MigrateAsync();
                            logger.LogInformation("✅ Database migrations applied successfully");
                        }
                        else
                        {
                            logger.LogWarning("⚠️ Pending migrations detected in production. Please apply manually.");
                        }
                    }
                    else
                    {
                        logger.LogInformation("✅ Database is up to date");
                    }

                    // Test a simple query
                    try
                    {
                        var userCount = await context.Users.CountAsync();
                        logger.LogInformation("👥 Database contains {UserCount} users", userCount);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("⚠️ Could not query users table: {Message}", ex.Message);
                        
                        if (app.Environment.IsDevelopment())
                        {
                            logger.LogInformation("🔧 Creating database and applying migrations...");
                            await context.Database.EnsureCreatedAsync();
                            await context.Database.MigrateAsync();
                        }
                    }
                }
                else
                {
                    logger.LogError("❌ Failed to connect to database after 3 attempts");
                    
                    if (!app.Environment.IsDevelopment())
                    {
                        throw new InvalidOperationException("Database is not accessible and application cannot start in production mode");
                    }
                    
                    logger.LogWarning("⚠️ Running in development mode without database connection");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💥 Database initialization failed: {Message}", ex.Message);
                
                if (!app.Environment.IsDevelopment())
                {
                    throw;
                }
                
                logger.LogWarning("🚨 Continuing in development mode despite database issues");
                logger.LogInformation("💡 Suggested fixes:");
                logger.LogInformation("   1. Check if SQL Server LocalDB is installed");
                logger.LogInformation("   2. Verify connection string format");
                logger.LogInformation("   3. Check Windows SQL Server services");
                logger.LogInformation("   4. Run: sqllocaldb info");
            }

            return app;
        }
    }
}