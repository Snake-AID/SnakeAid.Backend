using Microsoft.EntityFrameworkCore;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Transaction;
using SnakeAid.Core.Responses.Transaction;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        public TransactionService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedData<TransactionResponse>> GetTransactionsAsync(GetTransactionsRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            var hasTypeFilter = TryMapGroupToTransactionTypes(request.TransType, out var matchedTypes);

            var pagedData = await _unitOfWork.GetRepository<Transaction>()
                .GetPagingListAsync(
                    selector: t => new TransactionResponse
                    {
                        Id = t.Id,
                        UserName = t.User.UserName,
                        FullName = t.User.FullName,
                        ReferenceId = t.ReferenceId,
                        Amount = t.Amount,
                        Currency = t.Currency,
                        TransactionType = t.TransactionType,
                        Description = t.Description,
                        PaymentMethod = t.PaymentMethod,
                        ExternalTransactionId = t.ExternalTransactionId,
                        CreatedAt = t.CreatedAt
                    },
                    predicate: t =>
                        (!request.UserId.HasValue || t.UserId == request.UserId.Value)
                        && (!hasTypeFilter || matchedTypes!.Contains(t.TransactionType)),
                    orderBy: q => q.OrderByDescending(t => t.CreatedAt),
                    include: q => q.Include(t => t.User),
                    page: request.PageNumber,
                    size: request.PageSize,
                    cancellationToken: ct);

            return new PagedData<TransactionResponse>
            {
                Items = pagedData.Items,
                Meta = pagedData.Meta
            };
        }

        public async Task<TransactionResponse?> GetTransactionDetailAsync(Guid transactionId, CancellationToken ct = default)
        {
            return await _unitOfWork.GetRepository<Transaction>()
                .FirstOrDefaultAsync(
                    selector: t => new TransactionResponse
                    {
                        Id = t.Id,
                        UserName = t.User.UserName,
                        FullName = t.User.FullName,
                        ReferenceId = t.ReferenceId,
                        Amount = t.Amount,
                        Currency = t.Currency,
                        TransactionType = t.TransactionType,
                        Description = t.Description,
                        PaymentMethod = t.PaymentMethod,
                        ExternalTransactionId = t.ExternalTransactionId,
                        CreatedAt = t.CreatedAt
                    },
                    predicate: t => t.Id == transactionId,
                    include: q => q.Include(t => t.User),
                    asNoTracking: true,
                    cancellationToken: ct);
        }

        private static bool TryMapGroupToTransactionTypes(string? transType, out TransactionType[]? mappedTypes)
        {
            if (string.IsNullOrWhiteSpace(transType))
            {
                mappedTypes = null;
                return false;
            }

            var normalized = NormalizeTransType(transType);

            mappedTypes = normalized switch
            {
                "consultation" =>
                [
                    TransactionType.ConsultationPayment,
                    TransactionType.ExpertPayout,
                    TransactionType.ConsultationRefund
                ],
                "snakecatching" or "snakecathcing" =>
                [
                    TransactionType.CatchingPayment,
                    TransactionType.CatcherPayout,
                    TransactionType.CatchingRefund,
                    TransactionType.CatchingDeposit
                ],
                "snakebiteincident" =>
                [
                    TransactionType.SnakebiteIncidentPayment,
                    TransactionType.SnakebiteIncidentRefund
                ],
                "system" =>
                [
                    TransactionType.PlatformFee,
                    TransactionType.WalletTopup,
                    TransactionType.WalletWithdraw,
                    TransactionType.AdminAdjustment,
                    TransactionType.EscrowHold,
                    TransactionType.EscrowRelease
                ],
                _ => throw new BadRequestException("Invalid transType. Supported values: consultation, snake catching, snakebite incident, system.")
            };

            return true;
        }

        private static string NormalizeTransType(string transType)
        {
            return new string(transType
                .Trim()
                .ToLowerInvariant()
                .Where(c => c != ' ' && c != '-' && c != '_')
                .ToArray());
        }
    }
}
