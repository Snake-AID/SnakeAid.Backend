using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class RescuerLocationService : IRescuerLocationService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<RescuerLocationService> _logger;
        private readonly TimeSpan _throttleInterval;

        public RescuerLocationService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            IMemoryCache memoryCache,
            ILogger<RescuerLocationService> logger,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
            _logger = logger;

            var seconds = configuration.GetValue<int>("LocationUpdate:ThrottleIntervalSeconds", 10);
            _throttleInterval = TimeSpan.FromSeconds(seconds);
        }

        public async Task UpdateLocationAsync(Guid rescuerId, double latitude, double longitude, double? accuracy, double? speed, double? heading)
        {
            // 1. Validation
            if (latitude < -90 || latitude > 90)
            {
                _logger.LogWarning("Invalid latitude {Lat} for rescuer {RescuerId}", latitude, rescuerId);
                return; // Or throw ArgumentException
            }
            if (longitude < -180 || longitude > 180)
            {
                _logger.LogWarning("Invalid longitude {Lng} for rescuer {RescuerId}", longitude, rescuerId);
                return;
            }

            // 2. Throttling
            string cacheKey = $"LocUpdate_{rescuerId}";
            if (_memoryCache.TryGetValue(cacheKey, out DateTime lastUpdate))
            {
                if (DateTime.UtcNow - lastUpdate < _throttleInterval)
                {
                    // Rate limited - skip update
                    // We log at Debug level to avoid spamming logs
                    _logger.LogDebug("Location update throttled for rescuer {RescuerId}", rescuerId);
                    return;
                }
            }

            // Update cache immediately
            _memoryCache.Set(cacheKey, DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // 3. Persistence
            try
            {
                var repo = _unitOfWork.GetRepository<RescuerProfile>();
                var profile = await repo.FirstOrDefaultAsync(predicate: p => p.AccountId == rescuerId);

                if (profile == null)
                {
                    _logger.LogWarning("RescuerProfile not found for {RescuerId} during location update", rescuerId);
                    return;
                }

                // Update location
                // Create Point with SRID 4326 (WGS 84)
                profile.LastLocation = new Point(longitude, latitude) { SRID = 4326 };
                profile.LastLocationUpdate = DateTime.UtcNow;

                // We are not tracking speed/heading/accuracy in profile yet (LT-1 scope)
                // In LT-2, we might push these to Redis stream or history table.

                repo.Update(profile);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Updated location for rescuer {RescuerId} to ({Lat}, {Lng})", rescuerId, latitude, longitude);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist location for rescuer {RescuerId}", rescuerId);
                // We might choose not to throw here to keep signalR connection alive/stable
            }
        }
    }
}
