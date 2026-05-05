using System.Text.Json.Serialization;

namespace SnakeAid.Core.Responses.Consultation.History;

public class MyConsultationHistoryResponse : MyConsultationHistoryUnionResponse
{
    [JsonIgnore]
    public override string Kind => "consultation";
    public Guid ConsultationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid ExpertId { get; set; }
    public string? ExpertName { get; set; }
    public string? ExpertAvatarUrl { get; set; }
    public string? RoomId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Price { get; set; }
    public string? ProblemDescription { get; set; }
    public string? CustomerReport { get; set; }
    public DateTime? CustomerReportSubmittedAt { get; set; }
    public Guid? BookingId { get; set; }
    public DateTime? SlotStartTime { get; set; }
    public DateTime? SlotEndTime { get; set; }
    public Guid? EmergencyRequestId { get; set; }

    public override DateTime? HistorySortTime => StartTime;
}
