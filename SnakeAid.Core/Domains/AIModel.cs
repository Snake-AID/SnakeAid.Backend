using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class AIModel : BaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string EndpointUrl { get; set; }
        public string? ApiKey { get; set; }
        public string? ModelParameters { get; set; }  // JSON config

        // Model performance metrics
        public decimal Accuracy { get; set; }
        public decimal Precision { get; set; }
        public decimal Recall { get; set; }
        public int TotalPredictions { get; set; } = 0;
        public int CorrectPredictions { get; set; } = 0;

        // Model status
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public DateTime? DeployedAt { get; set; }
        public DateTime? RetiredAt { get; set; }

        // Supported features
        public bool SupportsVenomDetection { get; set; } = true;
        public bool SupportsSpeciesIdentification { get; set; } = true;
        public bool SupportsBoundingBox { get; set; } = false;
        public bool SupportsMultipleSnakes { get; set; } = false;

        // Navigation properties
        public ICollection<SnakeAIRecognitionResult> RecognitionResults { get; set; } = new List<SnakeAIRecognitionResult>();
        public ICollection<AISnakeClassMapping> ClassMappings { get; set; } = new List<AISnakeClassMapping>();
    }
}
