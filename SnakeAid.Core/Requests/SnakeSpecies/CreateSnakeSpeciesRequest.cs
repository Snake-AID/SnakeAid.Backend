using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.SnakeSpecies;

public class CreateSnakeSpeciesRequest
{
    [Required]
    [MaxLength(500)]
    public string ScientificName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string CommonName { get; set; } = string.Empty;

    [Required]
    public Guid MediaId { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string IdentificationSummary { get; set; } = string.Empty;

    public PrimaryVenomType? PrimaryVenomType { get; set; }

    public IdentificationFeature? Identification { get; set; }

    public List<SymptomTimeline>? SymptomsByTime { get; set; }

    public FirstAidOverride? FirstAidGuidelineOverride { get; set; }

    [Range(0.0, 10.0)]
    public float RiskLevel { get; set; }

    public bool IsVenomous { get; set; }

    public bool IsActive { get; set; } = true;

    public List<int>? VenomIds { get; set; }

    public List<int>? AntivenomIds { get; set; }

    public List<string>? AlternativeNames { get; set; }
}
