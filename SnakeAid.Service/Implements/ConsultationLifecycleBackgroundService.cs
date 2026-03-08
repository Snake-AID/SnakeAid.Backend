using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Helpers;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class ConsultationLifecycleBackgroundService : BackgroundService
{
    private const string LifecycleWorkerLockName = "consultation:lifecycle:worker";
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ConsultationLifecycleBackgroundService> _logger;

    public ConsultationLifecycleBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ConsultationLifecycleBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<SnakeAidDbContext>>();
                var paymentService = scope.ServiceProvider.GetRequiredService<IConsultationPaymentService>();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var dbContext = unitOfWork.Context;
                var connection = dbContext.Database.GetDbConnection();

                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync(stoppingToken);
                }

                var acquired = await PostgresAdvisoryLockHelper.TryAcquireSessionLockAsync(
                    dbContext,
                    LifecycleWorkerLockName,
                    stoppingToken);

                if (!acquired)
                {
                    _logger.LogDebug("Skipped consultation lifecycle sweep because another replica currently owns the worker lock.");
                    await connection.CloseAsync();
                    continue;
                }

                try
                {
                    var expiredCount = await paymentService.ExpireEmergencyRequestsAsync(stoppingToken);
                    var completedCount = await bookingService.AutoCompleteElapsedScheduledConsultationsAsync(stoppingToken);

                    if (expiredCount > 0 || completedCount > 0)
                    {
                        _logger.LogInformation(
                            "Consultation lifecycle sweep completed. ExpiredEmergencyRequests={ExpiredCount}, AutoCompletedScheduledConsultations={CompletedCount}",
                            expiredCount,
                            completedCount);
                    }
                }
                finally
                {
                    await PostgresAdvisoryLockHelper.ReleaseSessionLockAsync(
                        dbContext,
                        LifecycleWorkerLockName,
                        stoppingToken);
                    await connection.CloseAsync();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Consultation lifecycle sweep failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
