using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Core.Responses.Consultation;

namespace SnakeAid.Service.Interfaces;

public interface IConsultationPaymentService
{
    Task<ConsultationPaymentResponse> PayScheduledBookingAsync(
        Guid userId,
        Guid bookingId,
        ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<ConsultationPaymentResponse> PayEmergencyRequestAsync(
        Guid userId,
        Guid requestId,
        ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<ConsultationPaymentResponse> ConfirmConsultationPaymentAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task<PayOsWebhookResponse> ConfirmConsultationPaymentByOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken = default);

    Task<PayOsWebhookResponse> ProcessConsultationWebhookAsync(
        string rawPayload,
        CancellationToken cancellationToken = default);

    Task<bool> IsConsultationPayOsOrderCodeAsync(
        long orderCode,
        CancellationToken cancellationToken = default);

    Task<bool> RefundEmergencyEscrowAsync(
        Guid requestId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<bool> RefundScheduledBookingAsync(
        Guid bookingId,
        Guid receiverId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<bool> CancelPendingScheduledBookingPaymentAsync(
        Guid bookingId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default);

    Task<bool> SettleConsultationEscrowAsync(
        Guid consultationId,
        CancellationToken cancellationToken = default);
}
