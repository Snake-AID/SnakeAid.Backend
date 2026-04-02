using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.Wallet
{
    public class WithdrawalResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string BankAccount { get; set; } // Masked for security
        public string BankName { get; set; }
        public string? BankBin { get; set; }
        public WalletWithdrawStatus Status { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? RejectionReason { get; set; }
        public string? VietQrPayload { get; set; }
        public string? VietQrImageBase64 { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
