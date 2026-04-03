using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Wallet
{
    public class ApproveWithdrawalRequest
    {
        [MaxLength(1000)]
        public string? AdminNotes { get; set; }
    }
}
