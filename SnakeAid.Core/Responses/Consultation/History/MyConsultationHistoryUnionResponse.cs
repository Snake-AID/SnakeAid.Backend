using System.Text.Json.Serialization;

namespace SnakeAid.Core.Responses.Consultation.History;

[JsonDerivedType(typeof(MyConsultationHistoryResponse))]
[JsonDerivedType(typeof(MyInstantConsultationRequestHistoryResponse))]
public abstract class MyConsultationHistoryUnionResponse
{
    public abstract string Kind { get; }

    public string Type { get; set; } = string.Empty;

    [JsonIgnore]
    public abstract DateTime? HistorySortTime { get; }
}
