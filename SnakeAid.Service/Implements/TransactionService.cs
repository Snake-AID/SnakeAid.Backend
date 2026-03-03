using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Transaction;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<TransactionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<TransactionResponse?> GetTransactionBySnakeCatchingRequestIdAsync(Guid snakeCatchingRequestId)
        {
            try
            {
                _logger.LogInformation("Fetching transaction for Snake Catching Request ID: {RequestId}", snakeCatchingRequestId);

                // Find transaction where ReferenceId = snakeCatchingRequestId
                // and TransactionType is related to snake catching (CatchingPayment, CatchingDeposit, etc.)
                var transaction = await _unitOfWork.GetRepository<Transaction>().FirstOrDefaultAsync(
                    predicate: t => t.ReferenceId == snakeCatchingRequestId &&
                                    (t.TransactionType == TransactionType.CatchingPayment ||
                                     t.TransactionType == TransactionType.CatcherPayout ||
                                     t.TransactionType == TransactionType.CatchingRefund ||
                                     t.TransactionType == TransactionType.CatchingDeposit),
                    orderBy: q => q.OrderByDescending(t => t.CreatedAt),
                    include: q => q.Include(t => t.User),
                    asNoTracking: true
                );

                if (transaction == null)
                {
                    _logger.LogWarning("No transaction found for Snake Catching Request ID: {RequestId}", snakeCatchingRequestId);
                    return null;
                }

                _logger.LogInformation("Transaction found: {TransactionId} for Snake Catching Request ID: {RequestId}", 
                    transaction.Id, snakeCatchingRequestId);

                // Map to response using Mapster
                var response = transaction.Adapt<TransactionResponse>();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transaction for Snake Catching Request ID: {RequestId}", snakeCatchingRequestId);
                throw;
            }
        }
    }
}
