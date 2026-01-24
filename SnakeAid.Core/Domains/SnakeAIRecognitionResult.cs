using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnakeAid.Core.Domains
{
    public class SnakeAIRecognitionResult : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(ReportMedia))]
        public Guid ReportMediaId { get; set; }

        [Required]
        [ForeignKey(nameof(AIModel))]
        public int AIModelId { get; set; }

        // AI Raw Result
        [Required]
        [MaxLength(200)]
        public string YoloClassName { get; set; }

        [Required]
        [Range(0.0, 1.0)]
        [Column(TypeName = "numeric(5,4)")]
        public decimal Confidence { get; set; }

        // Mapped Species
        [ForeignKey(nameof(DetectedSpecies))]
        public int? DetectedSpeciesId { get; set; }

        [Required]
        public bool IsMapped { get; set; } = false;

        // All detections
        [Column(TypeName = "jsonb")]
        public string? AllDetections { get; set; }

        [Required]
        public RecognitionStatus Status { get; set; } = RecognitionStatus.Processing;

        [ForeignKey(nameof(Expert))]
        public Guid? ExpertId { get; set; }

        public DateTime? ExpertVerifiedAt { get; set; }

        [ForeignKey(nameof(ExpertCorrectedSpecies))]
        public int? ExpertCorrectedSpeciesId { get; set; }

        [MaxLength(1000)]
        public string? ExpertNotes { get; set; }

        // Navigation properties
        public ReportMedia ReportMedia { get; set; }
        public AIModel AIModel { get; set; }
        public SnakeSpecies? DetectedSpecies { get; set; }
        public SnakeSpecies? ExpertCorrectedSpecies { get; set; }
        public AISnakeClassMapping? ClassMapping { get; set; }
        public Account? Expert { get; set; }
    }

    public enum RecognitionStatus
    {
        Processing = 0,
        Completed = 1,
        Failed = 2,
        ExpertVerified = 3,
        ExpertRejected = 4
    }
}