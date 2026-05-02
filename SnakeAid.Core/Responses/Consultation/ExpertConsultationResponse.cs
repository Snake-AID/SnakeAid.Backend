namespace SnakeAid.Core.Responses.Consultation;

public class ExpertConsultationResponse
{
    public Guid ConsultationId { get; set; }
    public string Type { get; set; } = string.Empty;       // "Scheduled" | "Emergency"
    public string Status { get; set; } = string.Empty;     // "Ongoing" | "Completed"
    public Guid UserId { get; set; }                        // CallerId
    public string? UserName { get; set; }                   // Caller's FullName
    public string? ExpertAvatarUrl { get; set; }
    public string? RoomId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? GrossPrice { get; set; }
    public decimal? NetPrice { get; set; }

    // Scheduled-specific
    public Guid? BookingId { get; set; }
    public DateTime? SlotStartTime { get; set; }
    public DateTime? SlotEndTime { get; set; }

    // Emergency-specific
    public Guid? EmergencyRequestId { get; set; }
}
