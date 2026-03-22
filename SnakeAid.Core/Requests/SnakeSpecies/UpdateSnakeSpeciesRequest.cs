using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.SnakeSpecies;

public class UpdateSnakeSpeciesRequest
{
    [MaxLength(500)]
    public string? ScientificName { get; set; }

    [MaxLength(200)]
    public string? Slug { get; set; }

    [MaxLength(500)]
    public string? CommonName { get; set; }

    public Guid? MediaId { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(2000)]
    public string? IdentificationSummary { get; set; }

    public PrimaryVenomType? PrimaryVenomType { get; set; }

    public IdentificationFeature? Identification { get; set; }

    public List<SymptomTimeline>? SymptomsByTime { get; set; }

    public FirstAidOverride? FirstAidGuidelineOverride { get; set; }

    [Range(0.0, 10.0)]
    public float? RiskLevel { get; set; }

    public bool? IsVenomous { get; set; }

    public bool? IsActive { get; set; }

    public List<int>? VenomIds { get; set; }

    public List<int>? AntivenomIds { get; set; }

    public List<string>? AlternativeNames { get; set; }
}
