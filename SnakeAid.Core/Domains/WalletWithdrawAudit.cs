using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnakeAid.Core.Domains
{
    public class WalletWithdrawAudit : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Withdrawal))]
        public Guid WithdrawalId { get; set; }

        public WalletWithdrawStatus? FromStatus { get; set; }

        [Required]
        public WalletWithdrawStatus ToStatus { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        public Guid? ActorUserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActorRole { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? DetailsJson { get; set; }

        public WalletWithdraw Withdrawal { get; set; } = null!;
    }
}
