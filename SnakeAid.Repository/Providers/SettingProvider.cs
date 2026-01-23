// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Caching.Memory;
// using Microsoft.Extensions.Logging;
// using SnakeAid.Core.Domains;
// using SnakeAid.Core.Converters;
// using SnakeAid.Repository.Data;
// using System.Collections.Concurrent;

// namespace SnakeAid.Repository.Providers
// {
//     public class SettingProvider : ISettingProvider
//     {
//         private readonly ApplicationDbContext _context;
//         private readonly ILogger<SettingProvider> _logger;
//         private readonly ConcurrentDictionary<string, SystemSetting> _cache;
//         private readonly object _lockObject = new();
//         private bool _isInitialized = false;

//         public SettingProvider(
//             ApplicationDbContext context,
//             ILogger<SettingProvider> logger)
//         {
//             _context = context;
//             _logger = logger;
//             _cache = new ConcurrentDictionary<string, SystemSetting>();
//         }

//         #region Sync Methods (Cache-based)
//         public T? GetSetting<T>(string key)
//         {
//             EnsureCacheInitialized();

//             if (!_cache.TryGetValue(key, out var setting))
//             {
//                 _logger.LogWarning("Setting '{Key}' not found in cache", key);
//                 return default(T);
//             }

//             try
//             {
//                 return SettingConverter.CastValue<T>(setting.Value, setting.ValueType);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to convert setting '{Key}' to type {Type}", key, typeof(T).Name);
//                 return default(T);
//             }
//         }

//         public T GetSetting<T>(string key, T defaultValue)
//         {
//             var result = GetSetting<T>(key);
//             return result ?? defaultValue;
//         }
//         #endregion

//         #region Async Methods (Database-based)
//         public async Task<T?> GetSettingAsync<T>(string key)
//         {
//             try
//             {
//                 var setting = await _context.SystemSettings
//                     .Where(s => s.SettingKey == key)
//                     .FirstOrDefaultAsync();

//                 if (setting == null)
//                 {
//                     _logger.LogWarning("Setting '{Key}' not found in database", key);
//                     return default(T);
//                 }

//                 return SettingConverter.CastValue<T>(setting.Value, setting.ValueType);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to get setting '{Key}' from database", key);
//                 return default(T);
//             }
//         }

//         public async Task<T> GetSettingAsync<T>(string key, T defaultValue)
//         {
//             var result = await GetSettingAsync<T>(key);
//             return result ?? defaultValue;
//         }
//         #endregion

//         #region Cache Management
//         public async Task LoadAllSettingsAsync()
//         {
//             try
//             {
//                 _logger.LogInformation("Loading all settings into cache...");

//                 var settings = await _context.SystemSettings.ToListAsync();

//                 lock (_lockObject)
//                 {
//                     _cache.Clear();
//                     foreach (var setting in settings)
//                     {
//                         _cache.TryAdd(setting.SettingKey, setting);
//                     }
//                     _isInitialized = true;
//                 }

//                 _logger.LogInformation("Loaded {Count} settings into cache", settings.Count);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to load settings into cache");
//                 throw;
//             }
//         }

//         public async Task RefreshSettingAsync(string key)
//         {
//             try
//             {
//                 var setting = await _context.SystemSettings
//                     .Where(s => s.SettingKey == key)
//                     .FirstOrDefaultAsync();

//                 if (setting != null)
//                 {
//                     _cache.AddOrUpdate(key, setting, (k, v) => setting);
//                     _logger.LogDebug("Refreshed setting '{Key}' in cache", key);
//                 }
//                 else
//                 {
//                     _cache.TryRemove(key, out _);
//                     _logger.LogDebug("Removed setting '{Key}' from cache", key);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to refresh setting '{Key}'", key);
//                 throw;
//             }
//         }

//         public bool IsSettingCached(string key)
//         {
//             return _cache.ContainsKey(key);
//         }

//         private void EnsureCacheInitialized()
//         {
//             if (!_isInitialized)
//             {
//                 lock (_lockObject)
//                 {
//                     if (!_isInitialized)
//                     {
//                         // Fallback: load synchronously if not initialized
//                         var settings = _context.SystemSettings.ToList();
//                         foreach (var setting in settings)
//                         {
//                             _cache.TryAdd(setting.SettingKey, setting);
//                         }
//                         _isInitialized = true;
//                         _logger.LogWarning("Settings loaded synchronously - consider initializing cache at startup");
//                     }
//                 }
//             }
//         }
//         #endregion
//     }
// }