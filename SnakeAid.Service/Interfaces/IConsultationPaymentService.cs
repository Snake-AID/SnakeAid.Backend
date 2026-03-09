using SnakeAid.Core.Requests.Consultation;
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

    Task<bool> RefundEmergencyEscrowAsync(
        Guid requestId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<int> ExpireEmergencyRequestsAsync(CancellationToken cancellationToken = default);

    Task<bool> SettleConsultationEscrowAsync(
        Guid consultationId,
        CancellationToken cancellationToken = default);
}
