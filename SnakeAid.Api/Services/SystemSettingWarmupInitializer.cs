using Microsoft.Extensions.Hosting;
using SnakeAid.Core.Services;

namespace SnakeAid.Api.Services;

public class SystemSettingWarmupInitializer : IHostedService
{
    private readonly ISystemSettingService _systemSettingService;
    private readonly ILogger<SystemSettingWarmupInitializer> _logger;

    public SystemSettingWarmupInitializer(
        ISystemSettingService systemSettingService,
        ILogger<SystemSettingWarmupInitializer> logger)
    {
        _systemSettingService = systemSettingService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _systemSettingService.LoadSettingsAsync();
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
