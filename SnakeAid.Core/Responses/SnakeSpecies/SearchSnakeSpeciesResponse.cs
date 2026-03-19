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

        public bool IsVenomous { get; set; }

        public PrimaryVenomType? PrimaryVenomType { get; set; }

        public List<VenomInfo> Venoms { get; set; } = new();

        public List<AntivenomInfo> Antivenoms { get; set; } = new();
    }

    public class VenomInfo
    {
        public string VenomType { get; set; }
        public string Description { get; set; }
    }

    public class AntivenomInfo
    {
        public string AntivenomName { get; set; }
        public string Manufacturer { get; set; }
        public string Effectiveness { get; set; }
    }
}