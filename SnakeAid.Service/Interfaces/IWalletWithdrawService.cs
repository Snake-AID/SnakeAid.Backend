using SnakeAid.Core.Domains;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IWalletWithdrawService
    {
        Task<WalletWithdraw> CreateWithdrawalRequestAsync(Guid userId, decimal amount, string bankAccount, string bankName, string accountHolderName, string bankBin);
        Task<WalletWithdraw?> GetWithdrawalByIdAsync(Guid withdrawalId);
        Task<IEnumerable<WalletWithdraw>> GetUserWithdrawalsAsync(Guid userId);
        Task<WalletWithdraw> CancelWithdrawalAsync(Guid withdrawalId, Guid userId);
        Task<WalletWithdraw> ApproveWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string? adminNotes);
        Task<WalletWithdraw> RejectWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string reason, string? adminNotes);
        Task<WalletWithdraw> CompleteWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string? adminNotes);
        Task<WalletWithdraw> FailWithdrawalAsync(Guid withdrawalId, Guid adminUserId, string reason, string? adminNotes);
        Task<IEnumerable<WalletWithdraw>> GetPendingWithdrawalsAsync();
        Task<IEnumerable<WalletWithdraw>> GetAllWithdrawalsAsync();
    }
}
