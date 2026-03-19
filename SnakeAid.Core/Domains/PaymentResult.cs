namespace SnakeAid.Core.Domains;

public class PaymentResult
{
    public Guid ReferenceId { get; set; }
    public PaymentReferenceType ReferenceType { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string Provider { get; set; } = "PayOS";
    public object? GatewayRawResponse { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}