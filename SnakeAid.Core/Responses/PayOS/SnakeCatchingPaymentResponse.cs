namespace SnakeAid.Core.Responses.PayOs;

public class SnakeCatchingPaymentResponse
{
    public Guid TransactionId { get; set; }
    public Guid SnakeCatchingRequestId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string Provider { get; set; } = "PayOS";
    public object? GatewayRawResponse { get; set; }
}
