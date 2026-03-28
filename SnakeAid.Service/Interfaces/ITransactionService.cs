using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Transaction;
using SnakeAid.Core.Responses.Transaction;

namespace SnakeAid.Service.Interfaces
{
    public interface ITransactionService
    {
        Task<PagedData<TransactionResponse>> GetTransactionsAsync(GetTransactionsRequest request, CancellationToken ct = default);
        Task<TransactionResponse?> GetTransactionDetailAsync(Guid transactionId, CancellationToken ct = default);
    }
}
