using System.Text.Json.Serialization;

namespace SnakeAid.Core.Responses.Consultation.History;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ExpertConsultationHistoryResponse), "consultation")]
[JsonDerivedType(typeof(ExpertInstantConsultationRequestHistoryResponse), "instant")]
public abstract class ExpertConsultationHistoryUnionResponse
{
    [JsonIgnore]
    public abstract string Kind { get; }

    public string Type { get; set; } = string.Empty;

    [JsonIgnore]
    public abstract DateTime? HistorySortTime { get; }
}
