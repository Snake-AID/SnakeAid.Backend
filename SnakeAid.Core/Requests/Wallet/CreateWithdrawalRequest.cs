using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Wallet
{
    public class CreateWithdrawalRequest
    {
        [Required]
        [Range(10000, 50000000)] // Min 10k VND, max 50M VND
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        [RegularExpression(@"^\d{8,20}$", ErrorMessage = "Bank account must be 8-20 digits")]
        public string BankAccount { get; set; }

        [Required]
        [MaxLength(100)]
        public string BankName { get; set; }

        [Required]
        [MaxLength(6)]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Bank bin must be 6 digits")]
        public string BankBin { get; set; }
    }
}