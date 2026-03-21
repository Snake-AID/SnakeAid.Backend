using SnakeAid.Core.Requests.Wallet;
using SnakeAid.Core.Responses.Wallet;

namespace SnakeAid.Service.Interfaces;

public interface IWalletTopupService
{
    Task<CreateWalletTopupResponse> CreateWalletTopupAsync(CreateWalletTopupRequest request, Guid currentUserId, CancellationToken cancellationToken);
}