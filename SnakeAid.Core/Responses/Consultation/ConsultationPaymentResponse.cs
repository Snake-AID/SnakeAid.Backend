using SnakeAid.Core.Requests.Consultation;

namespace SnakeAid.Core.Responses.Consultation;

public class ConsultationPaymentResponse
{
    public Guid ReferenceId { get; set; }
    public ConsultationPaymentReferenceType ReferenceType { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public ConsultationPaymentMethod PaymentMethod { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal UserWalletBalanceAfter { get; set; }
    public decimal SystemWalletBalanceAfter { get; set; }
    public DateTime PaidAtUtc { get; set; }
}

public enum ConsultationPaymentReferenceType
{
    ScheduledBooking = 0,
    EmergencyRequest = 1
}
