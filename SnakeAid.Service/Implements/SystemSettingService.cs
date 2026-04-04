using SnakeAid.Core.Converters;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Services;
using Microsoft.Extensions.Logging;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

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
            _logger.LogWarning("System setting '{Key}' not found.", key);
            return default;
        }

        if (!TryConvertSettingValue(setting.Value, setting.ValueType, out T? parsedValue))
        {
            _logger.LogWarning(
                "System setting '{Key}' has invalid value '{Value}' for type '{ValueType}'.",
                key,
                setting.Value,
                setting.ValueType);
            return default;
        }

        return parsedValue;
    }

    public T GetSetting<T>(string key, T defaultValue)
    {
        EnsureLoaded();

        if (!_cacheProvider.TryGet(key, out var setting) || setting == null)
        {
            _logger.LogWarning("System setting '{Key}' not found. Using default value.", key);
            return defaultValue;
        }

        if (!TryConvertSettingValue(setting.Value, setting.ValueType, out T? parsedValue) || parsedValue == null)
        {
            _logger.LogWarning(
                "System setting '{Key}' has invalid value '{Value}' for type '{ValueType}'. Using default value.",
                key,
                setting.Value,
                setting.ValueType);
            return defaultValue;
        }

        return parsedValue;
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

        if (!IsValidValueForType(value, valueType))
        {
            throw new ArgumentException($"Setting value '{value}' is invalid for type '{valueType}'.", nameof(value));
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

    private static bool IsValidValueForType(string value, SettingValueType valueType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return valueType switch
        {
            SettingValueType.String => true,
            SettingValueType.Int => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            SettingValueType.Decimal => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            SettingValueType.Boolean => bool.TryParse(value, out _),
            SettingValueType.Json => IsValidJson(value),
            _ => false
        };
    }

    private static bool IsValidJson(string value)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryConvertSettingValue<T>(string rawValue, SettingValueType valueType, out T? parsedValue)
    {
        parsedValue = default;

        if (!IsValidValueForType(rawValue, valueType))
        {
            return false;
        }

        try
        {
            object converted = valueType switch
            {
                SettingValueType.String => rawValue,
                SettingValueType.Int => int.Parse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture),
                SettingValueType.Decimal => decimal.Parse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture),
                SettingValueType.Boolean => bool.Parse(rawValue),
                SettingValueType.Json => JsonSerializer.Deserialize<T>(rawValue)!,
                _ => throw new NotSupportedException($"Unsupported setting value type: {valueType}")
            };

            if (valueType == SettingValueType.Json)
            {
                parsedValue = (T?)converted;
                return parsedValue != null;
            }

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (converted.GetType() == targetType)
            {
                parsedValue = (T)converted;
                return true;
            }

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(converted.GetType()))
            {
                var result = converter.ConvertFrom(null, CultureInfo.InvariantCulture, converted);
                if (result != null)
                {
                    parsedValue = (T)result;
                    return true;
                }
            }

            parsedValue = SettingConverter.CastValue<T>(rawValue, valueType);
            return parsedValue != null;
        }
        catch
        {
            parsedValue = default;
            return false;
        }
    }
}
