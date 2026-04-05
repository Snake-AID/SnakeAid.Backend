using SnakeAid.Core.Domains;
using SnakeAid.Repository.Interfaces;

namespace SnakeAid.Service.Services.PayOs;

/// <summary>
/// Looks up PayOS transaction descriptions by order code or transaction ID.
/// Used by PayOsController to determine which payment flow owns a given transaction.
/// </summary>
public class PayOsDescriptionLookup
{
    private readonly IUnitOfWork _unitOfWork;

    public PayOsDescriptionLookup(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string?> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.GetRepository<Transaction>()
            .FirstOrDefaultAsync(
                predicate: t => t.Id == transactionId,
                asNoTracking: true,
                cancellationToken: ct);
        return transaction?.Description;
    }

    public async Task<string?> GetByOrderCodeAsync(long orderCode, CancellationToken ct = default)
    {
        var orderCodeStr = orderCode.ToString();
        var transaction = await _unitOfWork.GetRepository<Transaction>()
            .FirstOrDefaultAsync(
                predicate: t => t.Description != null &&
                    (t.Description.StartsWith("TOPUP-" + orderCodeStr) ||
                     t.Description.StartsWith("SNAKEAID-" + orderCodeStr) ||
                     t.Description.StartsWith("INCIDENT-" + orderCodeStr) ||
                     t.Description.StartsWith("CONSULTPAY-" + orderCodeStr)),
                asNoTracking: true,
                cancellationToken: ct);
        return transaction?.Description;
    }
}
