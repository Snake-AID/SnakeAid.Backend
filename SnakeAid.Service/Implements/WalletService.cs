using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Responses.Wallet;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<WalletService> _logger;

        public WalletService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<WalletService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<WalletResponse> GetWalletByUserIdAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Fetching wallet for User ID: {UserId}", userId);

                var wallet = await _unitOfWork.GetRepository<Wallet>().FirstOrDefaultAsync(
                    predicate: w => w.UserId == userId,
                    asNoTracking: true
                );

                if (wallet == null)
                {
                    _logger.LogWarning("No wallet found for User ID: {UserId}", userId);
                    throw new NotFoundException($"Wallet not found for user with ID: {userId}");
                }

                _logger.LogInformation("Wallet found: {WalletId} for User ID: {UserId}", wallet.Id, userId);

                var response = wallet.Adapt<WalletResponse>();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching wallet for User ID: {UserId}", userId);
                throw;
            }
        }
    }
}
