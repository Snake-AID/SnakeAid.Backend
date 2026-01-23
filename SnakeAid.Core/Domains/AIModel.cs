using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class AIModel : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Version { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        public string EndpointUrl { get; set; }

        public string? ApiKey { get; set; }

        public string? ModelParameters { get; set; }  // JSON config

        // Model status
        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        public DateTime? DeployedAt { get; set; }

        public DateTime? RetiredAt { get; set; }


        // Navigation properties
        public ICollection<SnakeAIRecognitionResult> RecognitionResults { get; set; } = new List<SnakeAIRecognitionResult>();
        public ICollection<AISnakeClassMapping> ClassMappings { get; set; } = new List<AISnakeClassMapping>();
    }
}
