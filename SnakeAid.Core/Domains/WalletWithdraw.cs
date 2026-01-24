using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class WalletWithdraw : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey(nameof(Wallet))]
        public Guid WalletId { get; set; }

        [Required]
        [Range(0.01, 999999999.99)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(100)]
        public string BankAccount { get; set; }  // Số tài khoản ngân hàng

        [Required]
        [MaxLength(100)]
        public string BankName { get; set; }  // Tên ngân hàng

        [Required]
        public WalletWithdrawStatus Status { get; set; } = WalletWithdrawStatus.Pending;

        public DateTime? ProcessedAt { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }


        // Navigation properties
        public Account User { get; set; }
        public Wallet Wallet { get; set; }
    }

    public enum WalletWithdrawStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
    }
}