using System.Collections.Concurrent;
using SnakeAid.Core.Domains;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class SystemSettingCacheProvider : ISystemSettingCacheProvider
{
    private readonly ConcurrentDictionary<string, SystemSetting> _cache = new(StringComparer.OrdinalIgnoreCase);

    public void SetAll(IEnumerable<SystemSetting> settings)
    {
        _cache.Clear();

        foreach (var setting in settings)
        {
            _cache[setting.SettingKey] = setting;
        }
    }

    public void Set(SystemSetting setting)
    {
        _cache[setting.SettingKey] = setting;
    }

    public bool TryGet(string key, out SystemSetting? setting)
    {
        var found = _cache.TryGetValue(key, out var value);
        setting = value;
        return found;
    }

    public IReadOnlyCollection<SystemSetting> GetAll()
    {
        return _cache.Values.ToList();
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}
