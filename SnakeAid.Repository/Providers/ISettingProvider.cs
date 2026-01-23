namespace SnakeAid.Repository.Providers
{
    public interface ISettingProvider
    {
        // Sync methods - lấy từ cache (nhanh)
        T? GetSetting<T>(string key);
        T GetSetting<T>(string key, T defaultValue);

        // Async methods - lấy từ DB (chậm nhưng real-time)
        Task<T?> GetSettingAsync<T>(string key);
        Task<T> GetSettingAsync<T>(string key, T defaultValue);

        // Cache management
        Task LoadAllSettingsAsync();
        Task RefreshSettingAsync(string key);
        bool IsSettingCached(string key);
    }
}