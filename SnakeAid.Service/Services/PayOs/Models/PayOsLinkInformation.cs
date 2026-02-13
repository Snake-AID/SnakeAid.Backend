namespace SnakeAid.Service.Services.PayOs.Models;

public class PayOsLinkInformation
{
    public string Id { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public int AmountPaid { get; set; }
    public int AmountRemaining { get; set; }
    public string Status { get; set; } = string.Empty;
}
