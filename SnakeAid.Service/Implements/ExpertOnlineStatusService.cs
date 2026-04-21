using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class ExpertOnlineStatusService : IExpertOnlineStatusService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<ExpertOnlineStatusService> _logger;

        public ExpertOnlineStatusService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<ExpertOnlineStatusService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public Task<bool> SetOnlineAsync(string userId)
        {
            return SetOnlineStatusAsync(userId, isOnline: true);
        }

        public Task<bool> SetOfflineAsync(string userId)
        {
            return SetOnlineStatusAsync(userId, isOnline: false);
        }

        private async Task<bool> SetOnlineStatusAsync(string userId, bool isOnline)
        {
            if (!Guid.TryParse(userId, out var expertGuid))
            {
                _logger.LogWarning("Invalid GUID format for expertId: {UserId}", userId);
                return false;
            }

            var profileRepo = _unitOfWork.GetRepository<ExpertProfile>();
            var profile = await profileRepo.FirstOrDefaultAsync(
                predicate: p => p.AccountId == expertGuid,
                asNoTracking: false);

            if (profile == null)
            {
                _logger.LogWarning("ExpertProfile not found for userId: {UserId}", userId);
                return false;
            }

            if (profile.IsOnline == isOnline)
            {
                return false;
            }

            profile.IsOnline = isOnline;
            profile.UpdatedAt = DateTime.UtcNow;
            profileRepo.Update(profile);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "Expert {UserId} set to {AvailabilityState}.",
                userId,
                isOnline ? "ONLINE" : "OFFLINE");

            return true;
        }
    }
}
