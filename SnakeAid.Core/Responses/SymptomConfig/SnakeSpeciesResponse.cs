using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.SymptomConfig
{
    public class SnakeSpeciesResponse
    {
        public int Id { get; set; }

        public string ScientificName { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string CommonName { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string IdentificationSummary { get; set; } = string.Empty;

        public PrimaryVenomType? PrimaryVenomType { get; set; }

        public float RiskLevel { get; set; }

        public bool IsVenomous { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}