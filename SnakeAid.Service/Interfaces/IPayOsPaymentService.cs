using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;

namespace SnakeAid.Service.Interfaces;

public interface IPayOsPaymentService
{
    Task<SnakeCatchingPaymentResponse> CreatePaymentLinkAsync(CreateSnakeCatchingPaymentRequest request, Guid currentUserId, CancellationToken cancellationToken);
    Task<CancelPaymentLinkResponse> CancelPaymentLinkAsync(long orderCode, CancelPaymentLinkRequest request, CancellationToken cancellationToken);
    Task<PayOsWebhookResponse> ProcessWebhookAsync(string rawPayload, CancellationToken cancellationToken);
    Task<PayOsWebhookResponse> ConfirmPaymentAsync(Guid transactionId, CancellationToken cancellationToken);
    Task<PayOsWebhookResponse> ConfirmPaymentByOrderCodeAsync(long orderCode, CancellationToken cancellationToken);
    Task<TransferToRescuerResponse> TransferToRescuerAsync(TransferToRescuerRequest request, CancellationToken cancellationToken);
    Task<RefundTransactionResponse> RefundTransactionAsync(RefundTransactionRequest request, CancellationToken cancellationToken);
}
