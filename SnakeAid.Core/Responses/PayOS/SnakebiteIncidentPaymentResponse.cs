using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.PayOS
{
    /// <summary>
    /// Response model for snakebite incident payment operations.
    /// Used for both PayOS gateway and in-app wallet payments.
    /// </summary>
    public class SnakebiteIncidentPaymentResponse
    {
        /// <summary>
        /// The ID of the snakebite incident being paid for.
        /// </summary>
        public Guid SnakebiteIncidentId { get; set; }

        /// <summary>
        /// The transaction ID. Only populated when payment is successful.
        /// Null for PayOS payments before webhook confirmation.
        /// </summary>
        public Guid? TransactionId { get; set; }

        /// <summary>
        /// The PayOS order code. Only populated for PayOS payments.
        /// </summary>
        public long? OrderCode { get; set; }

        /// <summary>
        /// The payment amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// The currency code (default: VND).
        /// </summary>
        public string Currency { get; set; } = "VND";

        /// <summary>
        /// The payment status.
        /// </summary>
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// The payment provider (PayOS or Wallet).
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// The checkout URL for PayOS payments. Null for wallet payments.
        /// </summary>
        public string? CheckoutUrl { get; set; }

        /// <summary>
        /// The payment link ID from PayOS. Null for wallet payments.
        /// </summary>
        public string? PaymentLinkId { get; set; }

        /// <summary>
        /// The expiration time of the payment link. Null for wallet payments.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// The external transaction reference from the payment gateway.
        /// Populated after successful payment.
        /// </summary>
        public string? ExternalTransactionId { get; set; }

        /// <summary>
        /// The timestamp when payment was completed. Null if not yet paid.
        /// </summary>
        public DateTime? PaidAt { get; set; }
    }
}
