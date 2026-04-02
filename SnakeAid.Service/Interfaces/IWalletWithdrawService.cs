using SnakeAid.Core.Domains;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IWalletWithdrawService
    {
        Task<WalletWithdraw> CreateWithdrawalRequestAsync(Guid userId, decimal amount, string bankAccount, string bankName, string bankBin);
        Task<WalletWithdraw> GetWithdrawalByIdAsync(Guid withdrawalId);
        Task<IEnumerable<WalletWithdraw>> GetUserWithdrawalsAsync(Guid userId);
        Task<WalletWithdraw> CancelWithdrawalAsync(Guid withdrawalId, Guid userId);
        Task<WalletWithdraw> ApproveWithdrawalAsync(Guid withdrawalId, string adminUserId);
        Task<WalletWithdraw> RejectWithdrawalAsync(Guid withdrawalId, string adminUserId, string reason);
        Task<WalletWithdraw> CompleteWithdrawalAsync(Guid withdrawalId, string adminUserId);
        Task<WalletWithdraw> FailWithdrawalAsync(Guid withdrawalId, string adminUserId, string reason);
        Task<IEnumerable<WalletWithdraw>> GetPendingWithdrawalsAsync();
        Task<IEnumerable<WalletWithdraw>> GetAllWithdrawalsAsync();
    }
}