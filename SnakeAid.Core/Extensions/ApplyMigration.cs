using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace SnakeAid.Core.Extensions;

public static class ApplyMigration
{
    public static void ApplyMigrations<TContext>(this IApplicationBuilder app) where TContext : DbContext
    {
        using var scope = app.ApplicationServices.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<TContext>();

        try
        {
            var retryCount = 0;
            const int maxRetries = 5;

            while (retryCount < maxRetries)
                try
                {
                    context.Database.Migrate();
                    break;
                }
                catch (Exception)
                {
                    retryCount++;
                    if (retryCount == maxRetries)
                        throw;

                    // Wait before the next retry
                    Thread.Sleep(2000 * retryCount); // Exponential backoff
                }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }
}