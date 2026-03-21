using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.PayOs;

/// <summary>
/// Unified response model for cancelling a payment link.
/// Used across snakebite incident, snake catching request, and consultation payment flows.
/// </summary>
public class CancelPaymentLinkResponse
{
    /// <summary>
    /// Indicates whether the cancellation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The reference entity ID (SnakebiteIncidentId, SnakeCatchingRequestId, or BookingId depending on context).
    /// </summary>
    public Guid ReferenceId { get; set; }

    /// <summary>
    /// The PayOS order code.
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>
    /// The payment status after cancellation.
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// The original payment amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The amount that was paid before cancellation.
    /// </summary>
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// The remaining amount after cancellation.
    /// </summary>
    public decimal AmountRemaining { get; set; }

    /// <summary>
    /// A message describing the result.
    /// </summary>
    public string? Message { get; set; }
}
