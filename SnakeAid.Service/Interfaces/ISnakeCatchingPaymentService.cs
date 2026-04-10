using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;

namespace SnakeAid.Service.Interfaces;

public interface ISnakeCatchingPaymentService
{
    Task<SnakeCatchingPaymentResponse> CreateSnakeCatchingPaymentLinkAsync(
        CreateSnakeCatchingPaymentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken);

    Task<SnakeCatchingPaymentResponse> CreateWalletPaymentAsync(
        CreateSnakeCatchingPaymentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken);

    Task<CancelPaymentLinkResponse> CancelSnakeCatchingPaymentLinkAsync(
        long orderCode,
        CancelPaymentLinkRequest request,
        CancellationToken cancellationToken);

    Task<PayOsWebhookResponse> ProcessSnakeCatchingWebhookAsync(
        string rawPayload,
        CancellationToken cancellationToken);

    Task<PayOsWebhookResponse> ConfirmSnakeCatchingPaymentAsync(
        Guid transactionId,
        CancellationToken cancellationToken);

    Task<PayOsWebhookResponse> ConfirmSnakeCatchingPaymentByOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken);

    Task<TransferToRescuerResponse> TransferSnakeCatchingFundsToRescuerAsync(
        TransferToRescuerRequest request,
        CancellationToken cancellationToken);

    Task<RefundTransactionResponse> RefundSnakeCatchingTransactionAsync(
        RefundTransactionRequest request,
        CancellationToken cancellationToken);
}
