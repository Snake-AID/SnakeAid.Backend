using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.Wallet
{
    public class AdminWithdrawalResponse : WithdrawalResponse
    {
        public Guid? ProcessedByAdminId { get; set; }
        public string? AdminNotes { get; set; }
    }
}
