namespace SnakeAid.Core.Responses.PayOs;

public class SnakeCatchingRefundResponse
{
    public Guid TransactionId { get; set; }
    public Guid SnakeCatchingRequestId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public decimal UserWalletBalance { get; set; }
    public DateTime RefundedAt { get; set; }
    public string Message { get; set; } = "Refund completed successfully";
}
