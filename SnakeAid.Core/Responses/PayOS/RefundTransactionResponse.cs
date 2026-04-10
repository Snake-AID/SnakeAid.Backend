namespace SnakeAid.Core.Responses.PayOs;

public class RefundTransactionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid ReceiverId { get; set; }
    public decimal RefundAmount { get; set; }
    public Guid RefundTransactionId { get; set; }
    public decimal? SystemWalletBalanceBefore { get; set; }
    public decimal? SystemWalletBalanceAfter { get; set; }
    public decimal ReceiverWalletBalanceBefore { get; set; }
    public decimal ReceiverWalletBalanceAfter { get; set; }
    public DateTime RefundedAt { get; set; }
}
