using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class RescuerOnlineStatusService : IRescuerOnlineStatusService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<RescuerOnlineStatusService> _logger;

        public RescuerOnlineStatusService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<RescuerOnlineStatusService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task SetOnlineAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var rescuerGuid))
            {
                _logger.LogWarning("Invalid GUID format for userId: {UserId}", userId);
                return;
            }

            var rescuerProfile = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                predicate: r => r.AccountId == rescuerGuid,
                asNoTracking: false
            );

            if (rescuerProfile != null)
            {
                rescuerProfile.IsOnline = true;
                rescuerProfile.IsAvailable = true;
                rescuerProfile.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.GetRepository<RescuerProfile>().Update(rescuerProfile);
                await _unitOfWork.CommitAsync();
                _logger.LogInformation("Rescuer {UserId} set to ONLINE in database", userId);
            }
            else
            {
                _logger.LogWarning("RescuerProfile not found for userId: {UserId}", userId);
            }
        }

        public async Task SetOfflineAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var rescuerGuid))
            {
                _logger.LogWarning("Invalid GUID format for userId: {UserId}", userId);
                return;
            }

            var rescuerProfile = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                predicate: r => r.AccountId == rescuerGuid,
                asNoTracking: false
            );

            if (rescuerProfile != null)
            {
                rescuerProfile.IsOnline = false;
                rescuerProfile.IsAvailable = false;
                rescuerProfile.UpdatedAt = DateTime.UtcNow;
                rescuerProfile.LastLocationUpdate = DateTime.UtcNow;
                _unitOfWork.GetRepository<RescuerProfile>().Update(rescuerProfile);
                await _unitOfWork.CommitAsync();
                _logger.LogInformation("Rescuer {UserId} set to OFFLINE in database", userId);
            }
            else
            {
                _logger.LogWarning("RescuerProfile not found for userId: {UserId}", userId);
            }
        }
    }
}