namespace SnakeAid.Core.Responses.Consultation;

public class MyConsultationResponse
{
    public Guid ConsultationId { get; set; }
    public string Type { get; set; } = string.Empty; // "Scheduled" or "Emergency"
    public string Status { get; set; } = string.Empty; // "Ongoing" or "Completed"
    public Guid ExpertId { get; set; }
    public string? ExpertName { get; set; }
    public string? RoomId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Price { get; set; }
    public string? ProblemDescription { get; set; }
    public string? CustomerReport { get; set; }
    public DateTime? CustomerReportSubmittedAt { get; set; }

    // Scheduled-specific
    public Guid? BookingId { get; set; }
    public DateTime? SlotStartTime { get; set; }
    public DateTime? SlotEndTime { get; set; }

    // Emergency-specific
    public Guid? EmergencyRequestId { get; set; }
}
