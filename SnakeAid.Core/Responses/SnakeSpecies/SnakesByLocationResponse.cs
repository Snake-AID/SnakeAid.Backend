using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.SnakeSpecies
{
    public class SnakesByLocationResponse
    {
        public GeographicRegionDto Region { get; set; } = null!;

        public List<SnakeInRegionDto> Snakes { get; set; } = new();
    }

    public class GeographicRegionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class SnakeInRegionDto
    {
        public int Id { get; set; }
        public string ScientificName { get; set; } = string.Empty;
        public string CommonName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IdentificationSummary { get; set; }
        public PrimaryVenomType? PrimaryVenomType { get; set; }
        public float RiskLevel { get; set; }
        public bool IsVenomous { get; set; }

        // Region-specific metadata (from RegionSnakeMapping)
        public string CommonLevel { get; set; } = string.Empty; // "VeryCommon", "Common", "Uncommon", "Rare"
        public int Priority { get; set; }
        public string? DistributionNotes { get; set; }
    }
}
