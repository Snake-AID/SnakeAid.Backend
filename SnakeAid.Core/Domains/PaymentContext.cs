namespace SnakeAid.Core.Domains;

public class PaymentContext
{
    public Guid ReferenceId { get; set; }
    public PaymentReferenceType ReferenceType { get; set; }
    public decimal Amount { get; set; }
    public Guid SenderId { get; set; }
    public Guid? ReceiverId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ItemName { get; set; } = "Payment";
    public int Quantity { get; set; } = 1;
    public Dictionary<string, object>? Metadata { get; set; }
}