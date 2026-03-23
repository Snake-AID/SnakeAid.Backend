using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.PayOs;

/// <summary>
/// Response model for PayOS webhook processing and payment confirmation.
/// </summary>
public class PayOsWebhookResponse
{
    /// <summary>
    /// Indicates whether the payment was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// A message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the snakebite incident associated with this payment.
    /// </summary>
    public Guid SnakebiteIncidentId { get; set; }

    /// <summary>
    /// The transaction ID. Only populated when payment is successful.
    /// </summary>
    public Guid? TransactionId { get; set; }

    /// <summary>
    /// The PayOS order code.
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>
    /// The payment amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The payment status.
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// The transaction reference from PayOS.
    /// </summary>
    public string TransactionReference { get; set; } = string.Empty;

    /// <summary>
    /// The transaction date/time from PayOS.
    /// </summary>
    public DateTime? TransactionDateTime { get; set; }
}
