using System.Text.Json.Serialization;

namespace SnakeAid.Core.Responses.Consultation.History;

public class ExpertConsultationHistoryResponse : ExpertConsultationHistoryUnionResponse
{
    [JsonIgnore]
    public override string Kind => "consultation";
    public Guid ConsultationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserAvatarUrl { get; set; }
    public string? RoomId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? GrossPrice { get; set; }
    public decimal? NetPrice { get; set; }
    public Guid? BookingId { get; set; }
    public DateTime? SlotStartTime { get; set; }
    public DateTime? SlotEndTime { get; set; }
    public Guid? EmergencyRequestId { get; set; }

    public override DateTime? HistorySortTime => StartTime;
}
