namespace SnakeAid.Service.Services.PayOs.Models;

public class PayOsItemPayload
{
    public string Name { get; set; } = null!;
    public int Quantity { get; set; }
    public int Price { get; set; }
}
