namespace SnakeAid.Core.Domains
{
    public class WalletWithdraw
    {
        public Guid Id { get; set; }
        public float Amount { get; set; }
        public Guid UserId { get; set; }
        public Guid WalletId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public WalletWithdrawStatus Status { get; set; } = WalletWithdrawStatus.Pending;
        public DateTime? ProcessedAt { get; set; }
    }

    public enum WalletWithdrawStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
    }
}