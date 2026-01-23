namespace SnakeAid.Core.Domains
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid DocumentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TransactionType TransactionType { get; set; }

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