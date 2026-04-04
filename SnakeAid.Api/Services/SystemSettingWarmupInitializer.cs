using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SnakeAid.Core.Services;

namespace SnakeAid.Api.Services;

public class SystemSettingWarmupInitializer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemSettingWarmupInitializer> _logger;

    public SystemSettingWarmupInitializer(
        IServiceScopeFactory scopeFactory,
        ILogger<SystemSettingWarmupInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var systemSettingService = scope.ServiceProvider.GetRequiredService<ISystemSettingService>();
            await systemSettingService.LoadSettingsAsync();
            _logger.LogInformation("System settings warmup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm up system settings cache at startup");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
