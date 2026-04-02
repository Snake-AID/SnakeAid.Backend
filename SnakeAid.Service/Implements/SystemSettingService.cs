using SnakeAid.Core.Converters;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Services;
using Microsoft.Extensions.Logging;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class SystemSettingService : ISystemSettingService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly ISystemSettingCacheProvider _cacheProvider;
    private readonly ILogger<SystemSettingService> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private volatile bool _isLoaded;

    public SystemSettingService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        ISystemSettingCacheProvider cacheProvider,
        ILogger<SystemSettingService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheProvider = cacheProvider;
        _logger = logger;
    }

    public async Task LoadSettingsAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            var settings = await _unitOfWork
                .GetRepository<SystemSetting>()
                .GetListAsync(asNoTracking: true);

            _cacheProvider.SetAll(settings);
            _isLoaded = true;

            _logger.LogInformation("Loaded {Count} system setting(s) into cache", settings.Count);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public T? GetSetting<T>(string key)
    {
        EnsureLoaded();

        if (!_cacheProvider.TryGet(key, out var setting) || setting == null)
        {
            return default;
        }

        return SettingConverter.CastValue<T>(setting.Value, setting.ValueType);
    }

    public T GetSetting<T>(string key, T defaultValue)
    {
        var value = GetSetting<T>(key);
        return value == null ? defaultValue : value;
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        await EnsureLoadedAsync();

        if (_cacheProvider.TryGet(key, out var cached) && cached != null)
        {
            return cached;
        }

        var setting = await _unitOfWork
            .GetRepository<SystemSetting>()
            .FirstOrDefaultAsync(predicate: s => s.SettingKey == key, asNoTracking: true);

        if (setting != null)
        {
            _cacheProvider.Set(setting);
        }

        return setting;
    }

    public async Task<IReadOnlyCollection<SystemSetting>> GetAllAsync()
    {
        await EnsureLoadedAsync();
        return _cacheProvider.GetAll();
    }

    public async Task<SystemSetting> UpsertAsync(string key, string value, SettingValueType valueType, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Setting key is required.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Setting value is required.", nameof(value));
        }

        var repository = _unitOfWork.GetRepository<SystemSetting>();
        var existing = await repository.FirstOrDefaultAsync(predicate: s => s.SettingKey == key, asNoTracking: false);

        if (existing == null)
        {
            existing = new SystemSetting
            {
                SettingKey = key,
                Value = value,
                ValueType = valueType,
                Description = description
            };

            await repository.InsertAsync(existing);
        }
        else
        {
            existing.Value = value;
            existing.ValueType = valueType;
            existing.Description = description;
            repository.Update(existing);
        }

        await _unitOfWork.CommitAsync();

        await RefreshSettingAsync(key);

        return await GetByKeyAsync(key)
               ?? throw new InvalidOperationException($"Failed to load setting '{key}' after update.");
    }

    public async Task RefreshSettingAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var setting = await _unitOfWork
            .GetRepository<SystemSetting>()
            .FirstOrDefaultAsync(predicate: s => s.SettingKey == key, asNoTracking: true);

        if (setting == null)
        {
            _cacheProvider.Remove(key);
            _logger.LogInformation("Removed setting {Key} from cache because it no longer exists", key);
            return;
        }

        _cacheProvider.Set(setting);
        _logger.LogInformation("Refreshed system setting {Key}", key);
    }

    public async Task RefreshAllSettingsAsync()
    {
        await LoadSettingsAsync();
    }

    private void EnsureLoaded()
    {
        if (_isLoaded)
        {
            return;
        }

        LoadSettingsAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        await LoadSettingsAsync();
    }
}
