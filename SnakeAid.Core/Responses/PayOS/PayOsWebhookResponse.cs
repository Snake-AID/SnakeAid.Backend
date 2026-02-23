namespace SnakeAid.Core.Responses.PayOs;

public class PayOsWebhookResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid TransactionId { get; set; }
    public Guid? PayoutTransactionId { get; set; }
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public DateTime? TransactionDateTime { get; set; }
}
