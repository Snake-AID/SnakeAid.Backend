using SnakeAid.Core.Domains;

namespace SnakeAid.Service.Interfaces;

public interface IPaymentOrchestrator
{
    /// <summary>
    /// Creates a payment link for the given payment context
    /// </summary>
    Task<PaymentResult> CreatePaymentLinkAsync(
        PaymentContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Processes a webhook for the given payment reference
    /// </summary>
    Task<PaymentResult> ProcessWebhookAsync(
        Guid referenceId,
        PaymentReferenceType referenceType,
        string rawWebhookPayload,
        CancellationToken cancellationToken);

    /// <summary>
    /// Manually confirms a payment
    /// </summary>
    Task<PaymentResult> ConfirmPaymentAsync(
        Guid transactionId,
        CancellationToken cancellationToken);
}