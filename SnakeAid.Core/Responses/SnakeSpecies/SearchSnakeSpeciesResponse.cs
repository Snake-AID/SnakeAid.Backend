using SnakeAid.Core.Domains;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Responses.SnakeSpecies
{
    public class SearchSnakeSpeciesResponse
    {
        public int Id { get; set; }

        [MaxLength(500)]
        public string ScientificName { get; set; }

        [MaxLength(500)]
        public string CommonName { get; set; }

        [MaxLength(2000)]
        public string ImageUrl { get; set; }

        public List<string> GalleryUrls { get; set; } = new();

        public bool IsVenomous { get; set; }

        public string PrimaryVenomType { get; set; } = "None";

        public float RiskLevel { get; set; }

        public IdentificationInfo? Identification { get; set; }

        public List<VenomInfo> Venoms { get; set; } = new();

        public List<AntivenomInfo> Antivenoms { get; set; } = new();

        public FirstAidInfo? FirstAid { get; set; }

        public List<string> Tags { get; set; } = new();
    }

    public class IdentificationInfo
    {
        public List<string> PhysicalTraits { get; set; } = new();
        public List<string> Behaviors { get; set; } = new();
        public string? Habitat { get; set; }
    }

    public class FirstAidInfo
    {
        public string Mode { get; set; } = string.Empty;
        public List<string> DoItems { get; set; } = new();
        public List<string> DontItems { get; set; } = new();
    }

    public class VenomInfo
    {
        public int Id { get; set; }
        public string VenomType { get; set; }
        public string Description { get; set; }
    }

    public class AntivenomInfo
    {
        public int Id { get; set; }
        public string AntivenomName { get; set; }
        public string Manufacturer { get; set; }
        public string Effectiveness { get; set; }
    }
}
