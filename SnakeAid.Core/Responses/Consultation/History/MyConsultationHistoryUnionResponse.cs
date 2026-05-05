using System.Text.Json.Serialization;

namespace SnakeAid.Core.Responses.Consultation.History;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(MyConsultationHistoryResponse), "consultation")]
[JsonDerivedType(typeof(MyInstantConsultationRequestHistoryResponse), "instant")]
public abstract class MyConsultationHistoryUnionResponse
{
    [JsonIgnore]
    public abstract string Kind { get; }

    public string Type { get; set; } = string.Empty;

    [JsonIgnore]
    public abstract DateTime? HistorySortTime { get; }
}
