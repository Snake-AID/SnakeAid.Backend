using System.Text.Json.Serialization;

namespace SnakeAid.Core.Responses.Consultation.History;

public class ExpertInstantConsultationRequestHistoryResponse : ExpertConsultationHistoryUnionResponse
{
    [JsonIgnore]
    public override string Kind => "instant";
    public Guid InstantRequestId { get; set; }
    public string RequestStatus { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserAvatarUrl { get; set; }

    public override DateTime? HistorySortTime => RespondedAt ?? RequestedAt;
}
