using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Media;

namespace SnakeAid.Core.Responses.AIRecognition;

public class AIRecognitionSpeciesLiteResponse
{
    public int Id { get; set; }
    public string? CommonName { get; set; }
}

public class AIRecognitionAdminReportMediaListItemResponse
{
    public Guid RecognitionResultId { get; set; }
    public Guid ReportMediaId { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public MediaReferenceType ReferenceType { get; set; }
    public MediaPurpose Purpose { get; set; }
    public int AIModelId { get; set; }
    public string YoloClassName { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public AIRecognitionSpeciesLiteResponse? DetectedSpecies { get; set; }
    public AIRecognitionSpeciesLiteResponse? ExpertCorrectedSpecies { get; set; }
    public string? ExpertNotes { get; set; }
    public string? ExpertReviewerName { get; set; }
    public RecognitionStatus Status { get; set; }
    public bool NeedsExpertReview { get; set; }
    public DateTime? ExpertVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ExpertReviewItemResponse
{
    public ReportMediaResponse Media { get; set; } = new();

    public SnakeAIRecognitionResultResponse AIResult { get; set; } = new();
}

public class ExpertReviewedRecognitionItemResponse
{
    public ReportMediaResponse Media { get; set; } = new();

    public SnakeAIRecognitionResultResponse AIResult { get; set; } = new();

    public DateTime? ReviewedAt { get; set; }
}

public class AIRecognitionReviewDetailResponse
{
    public Guid RecognitionResultId { get; set; }
    public ReportMediaResponse Media { get; set; } = new();
    public SnakeAIRecognitionResultResponse AIResult { get; set; } = new();
}

public class ExpertReviewActionResponse
{
    public Guid RecognitionResultId { get; set; }
    public RecognitionStatus Status { get; set; }
    public Guid? ExpertId { get; set; }
    public DateTime? ExpertVerifiedAt { get; set; }
    public int? ExpertCorrectedSpeciesId { get; set; }
    public bool IsTrainingReady { get; set; }
}
