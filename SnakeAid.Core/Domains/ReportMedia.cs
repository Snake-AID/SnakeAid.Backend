using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Domains
{
    public class ReportMedia : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        // Reference to parent entity
        public Guid ReferenceId { get; set; }  // ID của entity cha
        public MediaReferenceType ReferenceType { get; set; }

        // Media info
        [Required]
        public string FileName { get; set; }

        [Required]
        public string MediaUrl { get; set; }

        [Required]
        public string ContentType { get; set; }

        public long FileSize { get; set; }

        [Required]
        public MediaPurpose Purpose { get; set; }


        // Upload batch info (để group các ảnh upload cùng lúc)
        public Guid? UploadBatchId { get; set; }
        public int? SequenceOrder { get; set; }  // Thứ tự trong batch

        // AI processing flags
        [Required]
        public bool RequiresAIProcessing { get; set; } = false;
        public bool IsProcessed { get; set; } = false;
        public DateTime? ProcessedAt { get; set; }

        // Navigation properties
        public ICollection<SnakeAIRecognitionResult> AIRecognitionResults { get; set; } = new List<SnakeAIRecognitionResult>();
    }

    public enum MediaReferenceType
    {
        CommunityReport = 0,
        SnakeCatchingRequest = 1,
        RescueMission = 2,
        SnakebiteIncident = 3
    }

    public enum MediaPurpose
    {
        Evidence = 0,           // Ảnh bằng chứng
        SnakeIdentification = 1, // Ảnh để AI nhận diện
        LocationProof = 2,      // Ảnh vị trí
        InjuryPhoto = 3,       // Ảnh vết thương
        BeforeAfter = 4        // Ảnh trước/sau
    }
}