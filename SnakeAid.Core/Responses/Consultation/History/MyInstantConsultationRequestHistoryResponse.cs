using System.Text.Json.Serialization;

namespace SnakeAid.Core.Responses.Consultation.History;

public class MyInstantConsultationRequestHistoryResponse : MyConsultationHistoryUnionResponse
{
    [JsonIgnore]
    public override string Kind => "instant";
    public Guid InstantRequestId { get; set; }
    public string RequestStatus { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public Guid ExpertId { get; set; }
    public string? ExpertName { get; set; }
    public string? ExpertAvatarUrl { get; set; }

    public override DateTime? HistorySortTime => RespondedAt ?? RequestedAt;
}
