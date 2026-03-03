using SnakeAid.Core.Responses.Wallet;

namespace SnakeAid.Service.Interfaces
{
    public interface IWalletService
    {
        Task<WalletResponse> GetWalletByUserIdAsync(Guid userId);
    }
}
