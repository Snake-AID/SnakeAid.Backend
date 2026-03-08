using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class ConsultationLifecycleBackgroundService : BackgroundService
{
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
                var paymentService = scope.ServiceProvider.GetRequiredService<IConsultationPaymentService>();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

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
