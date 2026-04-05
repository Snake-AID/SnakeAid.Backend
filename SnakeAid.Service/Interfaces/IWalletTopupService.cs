using SnakeAid.Core.Requests.Wallet;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Core.Responses.Wallet;

namespace SnakeAid.Service.Interfaces;

public interface IWalletTopupService
{
    Task<CreateWalletTopupResponse> CreateWalletTopupAsync(CreateWalletTopupRequest request, Guid currentUserId, CancellationToken cancellationToken);

    Task<PayOsWebhookResponse> ProcessWalletTopupWebhookAsync(
        string rawPayload,
        CancellationToken cancellationToken);

    Task<PayOsWebhookResponse> ConfirmWalletTopupAsync(
        Guid transactionId,
        CancellationToken cancellationToken);

    Task<PayOsWebhookResponse> ConfirmWalletTopupByOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken);
}
