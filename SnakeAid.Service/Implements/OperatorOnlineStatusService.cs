using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class OperatorOnlineStatusService : IOperatorOnlineStatusService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<OperatorOnlineStatusService> _logger;

        public OperatorOnlineStatusService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<OperatorOnlineStatusService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task SetOnDutyAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var operatorGuid))
            {
                _logger.LogWarning("Invalid GUID format for operatorId: {UserId}", userId);
                return;
            }

            var profile = await _unitOfWork.GetRepository<OperatorProfile>().FirstOrDefaultAsync(
                predicate: o => o.AccountId == operatorGuid,
                asNoTracking: false);

            if (profile == null)
            {
                _logger.LogWarning("OperatorProfile not found for userId: {UserId}", userId);
                return;
            }

            profile.IsOnDuty = true;
            profile.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<OperatorProfile>().Update(profile);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Operator {UserId} set to ON DUTY.", userId);
        }

        public async Task SetOffDutyAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var operatorGuid))
            {
                _logger.LogWarning("Invalid GUID format for operatorId: {UserId}", userId);
                return;
            }

            var profile = await _unitOfWork.GetRepository<OperatorProfile>().FirstOrDefaultAsync(
                predicate: o => o.AccountId == operatorGuid,
                asNoTracking: false);

            if (profile == null)
            {
                _logger.LogWarning("OperatorProfile not found for userId: {UserId}", userId);
                return;
            }

            profile.IsOnDuty = false;
            profile.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<OperatorProfile>().Update(profile);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Operator {UserId} set to OFF DUTY.", userId);
        }
    }
}
