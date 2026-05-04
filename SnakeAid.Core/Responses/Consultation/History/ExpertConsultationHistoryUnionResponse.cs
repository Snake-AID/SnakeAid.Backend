using System.Text.Json.Serialization;

namespace SnakeAid.Core.Responses.Consultation.History;

[JsonDerivedType(typeof(ExpertConsultationHistoryResponse))]
[JsonDerivedType(typeof(ExpertInstantConsultationRequestHistoryResponse))]
public abstract class ExpertConsultationHistoryUnionResponse
{
    public abstract string Kind { get; }

    public string Type { get; set; } = string.Empty;

    [JsonIgnore]
    public abstract DateTime? HistorySortTime { get; }
}
