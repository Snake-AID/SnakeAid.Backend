using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;

namespace SnakeAid.Service.Interfaces;

public interface IWalletPaymentService
{
    Task<SnakeCatchingPaymentResponse> CreateWalletPaymentAsync(CreateSnakeCatchingPaymentRequest request, Guid currentUserId, CancellationToken cancellationToken);
}
