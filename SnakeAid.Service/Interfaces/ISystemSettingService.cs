using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Services
{
    public interface ISystemSettingService
    {
        Task LoadSettingsAsync();
        T? GetSetting<T>(string key);
        T GetSetting<T>(string key, T defaultValue);
        Task<SystemSetting?> GetByKeyAsync(string key);
        Task<IReadOnlyCollection<SystemSetting>> GetAllAsync();
        Task<SystemSetting> UpsertAsync(string key, string value, SettingValueType valueType, string? description = null);
        Task RefreshSettingAsync(string key);
        Task RefreshAllSettingsAsync();
    }
}