using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.PayOs;

public class RefundTransactionRequest
{
    public Guid ReceiverId { get; set; }
    public Guid ReferenceId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
}
