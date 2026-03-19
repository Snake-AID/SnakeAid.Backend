using SnakeAid.Service.Services.PayOs.Models;

namespace SnakeAid.Service.Interfaces;

public interface IPayOsProvider
{
    /// <summary>
    /// Creates a new PayOS payment link with domain-neutral parameters
    /// </summary>
    Task<PayOsPaymentLinkResult> CreatePaymentLinkAsync(
        PayOsCreatePaymentRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cancels an existing PayOS payment link
    /// </summary>
    Task<PayOsPaymentLinkResult> CancelPaymentLinkAsync(
        long orderCode,
        string? cancellationReason,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves information about a PayOS payment link
    /// </summary>
    Task<PayOsLinkInformation?> GetPaymentLinkInformationAsync(
        long orderCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies and parses a PayOS webhook payload
    /// </summary>
    PayOsWebhookData VerifyWebhook(string rawPayload);
}