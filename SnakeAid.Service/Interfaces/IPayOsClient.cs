using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Interfaces;

public interface IPayOsClient
{
    Task<PayOsLinkCreated> CreatePaymentLinkAsync(PayOsLinkCreateContext context, CancellationToken cancellationToken);
    Task<PayOsLinkInformation?> GetPaymentLinkInformationAsync(long orderCode, CancellationToken cancellationToken);
    Task<PayOsLinkInformation?> CancelPaymentLinkAsync(long orderCode, string? reason, CancellationToken cancellationToken);
    Task ConfirmWebhookAsync(string webhookUrl, CancellationToken cancellationToken);
    PayOsWebhookData VerifyWebhook(string rawPayload);
}
