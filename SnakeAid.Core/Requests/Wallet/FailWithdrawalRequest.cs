using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Wallet
{
    public class FailWithdrawalRequest
    {
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? AdminNotes { get; set; }
    }
}
