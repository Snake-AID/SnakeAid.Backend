using SnakeAid.Core.Meta;

namespace SnakeAid.Core.Requests.Transaction;

public class GetTransactionsRequest : PaginationRequest
{
    public Guid? UserId { get; set; }

    public string? TransType { get; set; }
}