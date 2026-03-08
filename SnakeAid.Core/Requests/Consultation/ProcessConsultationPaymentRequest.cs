using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Consultation;

public class ProcessConsultationPaymentRequest
{
    [Required]
    public ConsultationPaymentMethod PaymentMethod { get; set; }
}

public enum ConsultationPaymentMethod
{
    WalletBalance = 0
}
