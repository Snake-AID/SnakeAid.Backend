using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Interfaces;

public interface IPaymentGateway
{
    Task<PayOsPaymentLinkResult> CreatePaymentLinkAsync(
        PayOsCreatePaymentRequest request,
        CancellationToken cancellationToken);

    Task<PayOsPaymentLinkResult> CancelPaymentLinkAsync(
        long orderCode,
        string? cancellationReason,
        CancellationToken cancellationToken);

    Task<PayOsLinkInformation?> GetPaymentLinkInformationAsync(
        long orderCode,
        CancellationToken cancellationToken);

    PayOsWebhookData VerifyWebhook(string rawPayload);
}
