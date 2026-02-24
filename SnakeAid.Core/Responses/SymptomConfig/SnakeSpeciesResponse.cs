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

        public string ScientificName { get; set; }

        public string Slug { get; set; }

        public string CommonName { get; set; }

        public string ImageUrl { get; set; }

        public string Description { get; set; }

        public string IdentificationSummary { get; set; }

        public PrimaryVenomType? PrimaryVenomType { get; set; }

        public float RiskLevel { get; set; }

        public bool IsVenomous { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}