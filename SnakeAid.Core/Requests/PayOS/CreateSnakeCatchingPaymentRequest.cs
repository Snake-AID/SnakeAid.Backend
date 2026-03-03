using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.PayOs;

public class CreateSnakeCatchingPaymentRequest
{
    public Guid SnakeCatchingRequestId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public TransactionType TransactionType { get; set; } = TransactionType.CatchingPayment;
}
