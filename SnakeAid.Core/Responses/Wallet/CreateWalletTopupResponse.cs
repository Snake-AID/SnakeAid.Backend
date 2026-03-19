using System;

namespace SnakeAid.Core.Responses.Wallet;

public class CreateWalletTopupResponse
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public int Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public long OrderCode { get; set; }
    public string? PaymentLinkId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Provider { get; set; } = "PayOS";
    public object? GatewayRawResponse { get; set; }
}