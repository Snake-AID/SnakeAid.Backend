using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.Consultation;

public class AdminConsultationResponse
{
    public Guid ConsultationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public Guid ExpertId { get; set; }
    public string? ExpertName { get; set; }
    public string? RoomId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Price { get; set; }
    public string? ProblemDescription { get; set; }
    public Guid? BookingId { get; set; }
    public string? BookingStatus { get; set; }
    public DateTime? BookedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public DateTime? CancelledAt { get; set; }
    public ConsultationBookingCancellationReason? CancellationReason { get; set; }
    public Guid? EmergencyRequestId { get; set; }
    public string? EmergencyRequestStatus { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? SlotStartTime { get; set; }
    public DateTime? SlotEndTime { get; set; }
}
