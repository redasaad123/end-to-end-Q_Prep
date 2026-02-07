using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ProjectAPI.Extensions
{
    public static class DatabaseExtensions
    {
        public static WebApplication InitializeDatabaseAsync(this WebApplication app)
        {
            // Run database initialization in background to avoid blocking app startup
            _ = Task.Run(async () =>
            {
                using var scope = app.Services.CreateScope();
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                var context = services.GetRequiredService<AppDbContext>();

                try
                {
                    logger.LogInformation("🔍 Starting database connectivity check...");
                    
                    // Use timeout for each connection attempt
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    bool canConnect = false;
                    
                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        try
                        {
                            canConnect = await context.Database.CanConnectAsync(cts.Token);
                            if (canConnect)
                            {
                                logger.LogInformation("✅ Database connection successful on attempt {Attempt}", attempt);
                                break;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            logger.LogWarning("⏱️ Database connection timeout on attempt {Attempt}", attempt);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning("⚠️ Database connection attempt {Attempt} failed: {Message}", attempt, ex.Message);
                        }
                        
                        if (attempt < 3)
                        {
                            logger.LogInformation("🔄 Retrying in {Delay} seconds...", 2 * attempt);
                            await Task.Delay(2000 * attempt, CancellationToken.None);
                        }
                    }

                    if (canConnect)
                    {
                        // Check for pending migrations
                        using var migrationCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(migrationCts.Token);
                        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(migrationCts.Token);
                        
                        logger.LogInformation("📊 Applied migrations: {Count}", appliedMigrations.Count());
                        
                        if (pendingMigrations.Any())
                        {
                            logger.LogInformation("🔄 Found {Count} pending migrations", pendingMigrations.Count());
                            
                            if (app.Environment.IsDevelopment())
                            {
                                logger.LogInformation("🚀 Applying migrations in development environment...");
                                await context.Database.MigrateAsync(migrationCts.Token);
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

                        // Test a simple query with timeout
                        try
                        {
                            using var queryCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            var userCount = await context.Users.CountAsync(queryCts.Token);
                            logger.LogInformation("👥 Database contains {UserCount} users", userCount);
                        }
                        catch (OperationCanceledException)
                        {
                            logger.LogWarning("⏱️ Database query timeout");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning("⚠️ Could not query users table: {Message}", ex.Message);
                        }
                    }
                    else
                    {
                        logger.LogWarning("❌ Failed to connect to database after 3 attempts");
                        logger.LogInformation("🚀 Application will continue running without database connection");
                        logger.LogInformation("💡 Database operations will fail until connection is restored");
                        logger.LogInformation("🔧 Suggested fixes:");
                        logger.LogInformation("   1. Check database server status: q_preps.mssql.somee.com");
                        logger.LogInformation("   2. Verify connection string credentials");
                        logger.LogInformation("   3. Check network connectivity");
                        logger.LogInformation("   4. Verify firewall settings");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "💥 Database initialization failed: {Message}", ex.Message);
                    logger.LogInformation("🚀 Application started without database connection");
                    logger.LogInformation("💡 Database operations will return error responses until connection is restored");
                }
            });

            return app;
        }
    }
}