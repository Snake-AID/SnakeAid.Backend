using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.AIRecognition;
using SnakeAid.Core.Responses.AIRecognition;

namespace SnakeAid.Service.Interfaces;

public interface IAIRecognitionReportMediaService
{
    Task<PagedData<ExpertReviewItemResponse>> GetExpertReviewQueueAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AIRecognitionReviewDetailResponse> GetExpertReviewQueueDetailAsync(
        Guid recognitionResultId,
        Guid expertId,
        CancellationToken cancellationToken = default);

    Task<PagedData<ExpertReviewedRecognitionItemResponse>> GetExpertReviewHistoryAsync(
        Guid expertId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ExpertReviewActionResponse> VerifyRecognitionAsync(
        Guid recognitionResultId,
        Guid expertId,
        ExpertVerifyRecognitionRequest request,
        CancellationToken cancellationToken = default);

    Task<ExpertReviewActionResponse> RejectRecognitionAsync(
        Guid recognitionResultId,
        Guid expertId,
        ExpertRejectRecognitionRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedData<AIRecognitionAdminReportMediaListItemResponse>> GetAdminReportMediaListAsync(
        int page,
        int pageSize,
        RecognitionStatus? status,
        decimal? minConfidence,
        decimal? maxConfidence,
        MediaReferenceType? referenceType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);

    Task<AIRecognitionAdminReportMediaListItemResponse> GetAdminReportMediaDetailAsync(
        Guid recognitionResultId,
        CancellationToken cancellationToken = default);
}
