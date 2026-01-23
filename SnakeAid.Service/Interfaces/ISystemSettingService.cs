using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Services
{
    public interface ISystemSettingService
    {
        Task LoadSettingsAsync();
        T? GetSetting<T>(string key);
        T GetSetting<T>(string key, T defaultValue);
        Task RefreshSettingAsync(string key);
        Task RefreshAllSettingsAsync();
    }
}