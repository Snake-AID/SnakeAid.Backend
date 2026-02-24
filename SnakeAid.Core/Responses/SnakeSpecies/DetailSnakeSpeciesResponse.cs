using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakeSpecies
{
    public class DetailSnakeSpeciesResponse
    {
        public int Id { get; set; }

        [MaxLength(500)]
        public string ScientificName { get; set; }

        [MaxLength(200)]
        public string Slug { get; set; }

        [MaxLength(500)]
        public string CommonName { get; set; }

        [MaxLength(2000)]
        public string ImageUrl { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [MaxLength(2000)]
        public string IdentificationSummary { get; set; }

        public PrimaryVenomType? PrimaryVenomType { get; set; }

        [Column(TypeName = "jsonb")]
        public IdentificationFeature? Identification { get; set; }

        [Column(TypeName = "jsonb")]
        public List<SymptomTimeline>? SymptomsByTime { get; set; }

        [Column(TypeName = "jsonb")]
        public FirstAidOverride? FirstAidGuidelineOverride { get; set; }

        [Range(0.0, 10.0)]
        public float RiskLevel { get; set; }

        public bool IsVenomous { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}
