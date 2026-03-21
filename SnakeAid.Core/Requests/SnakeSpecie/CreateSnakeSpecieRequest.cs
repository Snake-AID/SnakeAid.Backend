using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.SnakeSpecie
{
    public class CreateSnakeSpecieRequest
    {
        [Required]
        [MaxLength(500)]
        public string ScientificName { get; set; }

        

        [MaxLength(500)]
        public string CommonName { get; set; }

        [Required]
        [MaxLength(2000)]
        public string ImageUrl { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [MaxLength(2000)]
        public string IdentificationSummary { get; set; }

        public PrimaryVenomType? PrimaryVenomType { get; set; }

        public IdentificationFeature? Identification { get; set; }

        public List<SymptomTimeline>? SymptomsByTime { get; set; }

        public FirstAidOverride? FirstAidGuidelineOverride { get; set; }

        [Range(0.0, 10.0)]
        public float RiskLevel { get; set; }

        [Required]
        public bool IsVenomous { get; set; } = false;

        [Required]
        public bool IsActive { get; set; } = true;

        public ICollection<FilterSnakeMapping> FilterSnakeMappings { get; set; } = new List<FilterSnakeMapping>();
        public ICollection<SpeciesAntivenom> SpeciesAntivenoms { get; set; } = new List<SpeciesAntivenom>();
        public ICollection<SpeciesVenom> SpeciesVenoms { get; set; } = new List<SpeciesVenom>();
        public ICollection<SnakeCatchingTariff> SnakeCatchingTariffs { get; set; } = new List<SnakeCatchingTariff>();
        public ICollection<SnakeSpeciesName> AlternativeNames { get; set; } = new List<SnakeSpeciesName>();
        public ICollection<SnakeAid.Core.Domains.LibraryMedia> LibraryMedias { get; set; } = new List<SnakeAid.Core.Domains.LibraryMedia>();
    }
}
