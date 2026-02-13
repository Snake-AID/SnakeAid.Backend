using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Responses.PayOs;

namespace SnakeAid.Service.Interfaces;

public interface IPayOsPaymentService
{
    Task<SnakeCatchingPaymentResponse> CreatePaymentLinkAsync(CreateSnakeCatchingPaymentRequest request, CancellationToken cancellationToken);
    Task<CancelPaymentLinkResponse> CancelPaymentLinkAsync(long orderCode, CancelPaymentLinkRequest request, CancellationToken cancellationToken);
    Task<PayOsWebhookResponse> ProcessWebhookAsync(string rawPayload, CancellationToken cancellationToken);
    Task<PayOsWebhookResponse> ConfirmPaymentAsync(Guid transactionId, CancellationToken cancellationToken);
}
