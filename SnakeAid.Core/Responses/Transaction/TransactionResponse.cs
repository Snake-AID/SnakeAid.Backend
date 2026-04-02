using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.Transaction
{
    public class TransactionResponse
    {
        public Guid Id { get; set; }

        public string? UserName { get; set; }

        [Required]
        public Guid ReferenceId { get; set; }  

        [Required]
        [Range(0.01, 999999999.99)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "VND";  

        [Required]
        public TransactionType TransactionType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? PaymentMethod { get; set; }  // "CreditCard", "BankTransfer", "Wallet"

        [MaxLength(200)]
        public string? ExternalTransactionId { get; set; }  // ID từ payment gateway

        public DateTime? CreatedAt { get; set; }
    }
}
