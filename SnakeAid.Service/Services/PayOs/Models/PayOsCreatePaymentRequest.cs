namespace SnakeAid.Service.Services.PayOs.Models;

public class PayOsCreatePaymentRequest
{
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ItemName { get; set; } = "Payment";
    public int Quantity { get; set; } = 1;
}