namespace SnakeAid.Service.Services.PayOs.Models;

public class PayOsWebhookData
{
    public bool Success { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string PaymentLinkId { get; set; } = string.Empty;
    public string TransactionReference { get; set; } = string.Empty;
    public DateTime? TransactionDateTime { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string CounterAccountBankName { get; set; } = string.Empty;
    public string CounterAccountName { get; set; } = string.Empty;
    public string CounterAccountNumber { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
}
