using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Wallet
{
    public class CreateWithdrawalRequest
    {
        [Required]
        [Range(50000, 5000000)] // Min 50k VND, max 5M VND
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        [RegularExpression(@"^\d{8,20}$", ErrorMessage = "Bank account must be 8-20 digits")]
        public string BankAccount { get; set; }

        [Required]
        [MaxLength(100)]
        public string BankName { get; set; }

        [Required]
        [MaxLength(150)]
        public string AccountHolderName { get; set; }

        [Required]
        [MaxLength(6)]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Bank bin must be 6 digits")]
        public string BankBin { get; set; }
    }
}
