using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SymptomConfig;

namespace SnakeAid.Core.Responses.Media
{
    public class SnakeAIDetectMediaResponse
    {
        public Guid Id { get; set; }

        public string MediaUrl { get; set; } = string.Empty;

        public MediaReferenceType ReferenceType { get; set; }

        public MediaPurpose Purpose { get; set; }

        public bool IsProcessed { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public int? SequenceOrder { get; set; }

        public ICollection<SnakeAIRecognitionResultResponse> AIRecognitionResults { get; set; } = new List<SnakeAIRecognitionResultResponse>();
    }

    public class SnakeAIRecognitionResultResponse
    {
        public Guid Id { get; set; }

        public Guid ReportMediaId { get; set; }

        public string YoloClassName { get; set; } = string.Empty;

        public decimal Confidence { get; set; }

        public int? DetectedSpeciesId { get; set; }

        public bool IsMapped { get; set; }

        public RecognitionStatus Status { get; set; }

        public SnakeSpeciesResponse? DetectedSpecies { get; set; }

    }
}