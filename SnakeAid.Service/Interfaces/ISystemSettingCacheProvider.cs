using SnakeAid.Core.Domains;

namespace SnakeAid.Service.Interfaces;

public interface ISystemSettingCacheProvider
{
    void SetAll(IEnumerable<SystemSetting> settings);
    void Set(SystemSetting setting);
    bool TryGet(string key, out SystemSetting? setting);
    IReadOnlyCollection<SystemSetting> GetAll();
    void Remove(string key);
    void Clear();
}
