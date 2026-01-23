using System;
using System.Collections.Generic;
using System.Text.Json;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Domains
{
    public class SnakeAIRecognitionResult : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid ReportMediaId { get; set; }  // FK to ReportMedia
        public Guid AIModelId { get; set; }      // FK to AI_Model

        // Primary detection result
        public int? DetectedSpeciesId { get; set; }  // FK to SnakeSpecies
        public string YoloDetectedClass { get; set; }  // Raw YOLO class name
        public decimal Confidence { get; set; }

        // Bounding box coordinates (for YOLO detection)
        public decimal BboxX { get; set; }
        public decimal BboxY { get; set; }
        public decimal BboxWidth { get; set; }
        public decimal BboxHeight { get; set; }

        // Multiple detections in same image
        public JsonDocument AllDetections { get; set; }  // All YOLO detections as JSON
        public int DetectionCount { get; set; } = 1;  // Số rắn phát hiện trong ảnh

        // Processing metadata
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string ProcessingVersion { get; set; }
        public JsonDocument RawResponse { get; set; }  // Full AI model response
        public TimeSpan ProcessingDuration { get; set; }

        // Verification status
        public RecognitionStatus Status { get; set; } = RecognitionStatus.Pending;
        public bool IsVerifiedByExpert { get; set; } = false;
        public Guid? ExpertId { get; set; }
        public DateTime? ExpertVerifiedAt { get; set; }
        public string ExpertNotes { get; set; }

        // Navigation properties
        public ReportMedia ReportMedia { get; set; }
        public AIModel AIModel { get; set; }
        public SnakeSpecies DetectedSpecies { get; set; }
    }

    public enum RecognitionStatus
    {
        Pending = 0,
        Processed = 1,
        VerifiedCorrect = 2,
        VerifiedIncorrect = 3,
        Failed = 4,
        RequiresReview = 5
    }
}