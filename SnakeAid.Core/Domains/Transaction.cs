using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        public Guid ReferenceId { get; set; }  // Thay DocumentId thành ReferenceId cho rõ ràng

        [Required]
        [Range(0.01, 999999999.99)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "VND";  // ISO currency code

        [Required]
        public TransactionType TransactionType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? PaymentMethod { get; set; }  // "CreditCard", "BankTransfer", "Wallet"

        [MaxLength(200)]
        public string? ExternalTransactionId { get; set; }  // ID từ payment gateway

        public DateTime? CreatedAt { get; set; }


        // Navigation properties
        public Account User { get; set; }
    }

    public enum TransactionType
    {
        // Consultation related
        ConsultationPayment = 0,     // ReferenceId = BookingId
        ExpertPayout = 1,            // ReferenceId = ConsultationId
        ConsultationRefund = 2,      // ReferenceId = BookingId

        // Mission/Rescue related  
        MissionDonation = 10,        // ReferenceId = MissionId
        RescuerReward = 11,          // ReferenceId = MissionId

        // Snake Catching related
        CatchingPayment = 20,        // ReferenceId = CatchingRequestId
        CatcherPayout = 21,          // ReferenceId = CatchingId
        CatchingRefund = 22,         // ReferenceId = CatchingRequestId

        // System transactions
        PlatformFee = 30,            // Platform commission
        WalletTopup = 31,            // User nạp tiền
        WalletWithdraw = 32,         // User rút tiền
        AdminAdjustment = 33         // Admin điều chỉnh
    }
}