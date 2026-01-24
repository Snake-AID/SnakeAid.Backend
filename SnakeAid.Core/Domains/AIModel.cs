using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnakeAid.Core.Domains
{
    public class AIModel : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Version { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(500)]
        public string EndpointUrl { get; set; }

        [MaxLength(200)]
        public string? ApiKey { get; set; }

        [Column(TypeName = "jsonb")]
        public string? ModelParameters { get; set; }  // JSON config

        // Model status
        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public bool IsDefault { get; set; } = false;

        public DateTime? DeployedAt { get; set; }

        public DateTime? RetiredAt { get; set; }


        // Navigation properties
        public ICollection<SnakeAIRecognitionResult> RecognitionResults { get; set; } = new List<SnakeAIRecognitionResult>();
        public ICollection<AISnakeClassMapping> ClassMappings { get; set; } = new List<AISnakeClassMapping>();
    }
}
