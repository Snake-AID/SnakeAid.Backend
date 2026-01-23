using System.Text.Json;

namespace SnakeAid.Core.Domains
{
    public class SnakeSpecies : BaseEntity
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string CommonName { get; set; }
        public string ScientificName { get; set; }
        public string Description { get; set; }
        public float RiskLevel { get; set; }
        public List<VenomType> VenomTypes { get; set; } = new List<VenomType>();
        public JsonDocument KeyFeatures { get; set; }
        public JsonDocument FirstAidGuidelineOverride { get; set; }

        public ICollection<SpeciesFilterMapping> SpeciesFilterMappings { get; set; }
        public ICollection<SpeciesAntivenom> SpeciesAntivenoms { get; set; }
        public ICollection<SpeciesVenom> SpeciesVenoms { get; set; }
    }
}