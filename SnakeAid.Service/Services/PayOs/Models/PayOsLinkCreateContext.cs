namespace SnakeAid.Service.Services.PayOs.Models;

public class PayOsLinkCreateContext
{
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = null!;
    public IReadOnlyCollection<PayOsItemPayload> Items { get; set; } = Array.Empty<PayOsItemPayload>();
    public string ReturnUrl { get; set; } = null!;
    public string CancelUrl { get; set; } = null!;
}
