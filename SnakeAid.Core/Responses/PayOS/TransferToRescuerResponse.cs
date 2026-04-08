namespace SnakeAid.Core.Responses.PayOs;

public class TransferToRescuerResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid SnakeCatchingRequestId { get; set; }
    public Guid RescuerId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CommissionFee { get; set; }
    public decimal NetAmountToRescuer { get; set; }
    public Guid? TransferTransactionId { get; set; }
    public decimal? SystemWalletBalanceBefore { get; set; }
    public decimal? SystemWalletBalanceAfter { get; set; }
    public decimal? RescuerWalletBalanceBefore { get; set; }
    public decimal? RescuerWalletBalanceAfter { get; set; }
    public DateTime TransferredAt { get; set; }
}
