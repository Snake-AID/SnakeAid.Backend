using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.FirstAidGuideline;
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

        public string ScientificName { get; set; }


        public string Slug { get; set; }


        public string CommonName { get; set; }


        public string ImageUrl { get; set; }


        public string Description { get; set; }


        public string IdentificationSummary { get; set; }

        public string PrimaryVenomType { get; set; } = "None";


        public IdentificationFeature? Identification { get; set; }


        public List<SymptomTimeline>? SymptomsByTime { get; set; }

        public FirstAidGuidelineResponse? BaseFirstAidGuideline { get; set; }

        public FirstAidContent? EffectiveFirstAidGuideline { get; set; }

        public FirstAidOverride? FirstAidGuidelineOverride { get; set; }

        [Range(0.0, 10.0)]
        public float RiskLevel { get; set; }

        public bool IsVenomous { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public List<string> AlternativeNames { get; set; } = new();

        public List<VenomInfo> Venoms { get; set; } = new();

        public List<AntivenomInfo> Antivenoms { get; set; } = new();

        public int? PrimaryVenomTypeId { get; set; }
    }
}
