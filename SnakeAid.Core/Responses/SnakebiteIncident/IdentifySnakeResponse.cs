using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakeSpecies;

namespace SnakeAid.Core.Responses.SnakebiteIncident;

/// Response sau khi xác định được loài rắn cho snakebite incident
public class IdentifySnakeResponse
{
    public Guid IncidentId { get; set; }
    public int IdentifiedSnakeSpeciesId { get; set; }
    public SnakeIdentificationMethod IdentificationMethod { get; set; }
    public DateTime IdentifiedAt { get; set; }

    public SnakeSpeciesResponse Snake { get; set; } = new();

    /// Nếu là AI detection
    public Guid? AIRecognitionResultId { get; set; }
    public float? AIConfidence { get; set; }

    /// Nếu là filter questions
    public List<string> MatchedSnakes { get; set; } = new();
}