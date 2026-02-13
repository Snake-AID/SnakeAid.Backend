namespace SnakeAid.Core.Responses.PayOs;

public class CancelPaymentLinkResponse
{
    public long OrderCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Amount { get; set; }
    public int AmountPaid { get; set; }
    public int AmountRemaining { get; set; }
    public string Message { get; set; } = string.Empty;
}
