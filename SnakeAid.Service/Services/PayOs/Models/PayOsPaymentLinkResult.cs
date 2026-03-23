namespace SnakeAid.Service.Services.PayOs.Models;

public class PayOsPaymentLinkResult
{
    public long OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRemaining { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}