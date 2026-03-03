using SnakeAid.Core.Responses.Transaction;

namespace SnakeAid.Service.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse?> GetTransactionBySnakeCatchingRequestIdAsync(Guid snakeCatchingRequestId);
    }
}
