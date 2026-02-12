namespace SnakeAid.Service.Services.PayOs.Models;

public class PayOsLinkCreated
{
    public long OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
}
