using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Constants;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.AIRecognition;
using SnakeAid.Core.Responses.AIRecognition;
using SnakeAid.Core.Services;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Extensions;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class AIRecognitionReportMediaService : IAIRecognitionReportMediaService
{
    private const decimal DefaultLowConfidenceThreshold = 0.70m;

    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly ISystemSettingService _systemSettingService;
    private readonly ILogger<AIRecognitionReportMediaService> _logger;

    public AIRecognitionReportMediaService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        ISystemSettingService systemSettingService,
        ILogger<AIRecognitionReportMediaService> logger)
    {
        _unitOfWork = unitOfWork;
        _systemSettingService = systemSettingService;
        _logger = logger;
    }

    public async Task<PagedData<ExpertReviewItemResponse>> GetExpertReviewQueueAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var threshold = GetLowConfidenceThreshold();

        var query = BuildRecognitionBaseQuery()
            .Where(r => r.Status == RecognitionStatus.Completed && r.Confidence < threshold);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ExpertReviewItemResponse
            {
                Media = new Core.Responses.Media.ReportMediaResponse
                {
                    Id = r.ReportMedia.Id,
                    MediaUrl = r.ReportMedia.MediaUrl,
                    FileName = r.ReportMedia.FileName,
                    ContentType = r.ReportMedia.ContentType,
                    FileSize = r.ReportMedia.FileSize,
                    ReferenceType = r.ReportMedia.ReferenceType,
                    Purpose = r.ReportMedia.Purpose,
                    RequiresAIProcessing = r.ReportMedia.RequiresAIProcessing
                },
                AIResult = new Core.Responses.Media.SnakeAIRecognitionResultResponse
                {
                    Id = r.Id,
                    ReportMediaId = r.ReportMediaId,
                    YoloClassName = r.YoloClassName,
                    Confidence = r.Confidence,
                    DetectedSpeciesId = r.DetectedSpeciesId,
                    IsMapped = r.IsMapped,
                    Status = r.Status,
                    DetectedSpecies = r.DetectedSpeciesId.HasValue
                        ? new Core.Responses.SnakeSpecies.SnakeSpeciesResponse
                        {
                            Id = r.DetectedSpecies!.Id,
                            ScientificName = r.DetectedSpecies.ScientificName,
                            Slug = r.DetectedSpecies.Slug,
                            CommonName = r.DetectedSpecies.CommonName,
                            ImageUrl = r.DetectedSpecies.ImageUrl,
                            Description = r.DetectedSpecies.Description,
                            IdentificationSummary = r.DetectedSpecies.IdentificationSummary,
                            PrimaryVenomType = r.DetectedSpecies.GetPrimaryVenomTypeLabel(),
                            RiskLevel = r.DetectedSpecies.RiskLevel,
                            IsVenomous = r.DetectedSpecies.IsVenomous,
                            IsActive = r.DetectedSpecies.IsActive
                        }
                        : null
                }
            })
            .ToPaginatedResponse(page, pageSize);
    }

    public async Task<AIRecognitionReviewDetailResponse> GetExpertReviewQueueDetailAsync(
        Guid recognitionResultId,
        Guid expertId,
        CancellationToken cancellationToken = default)
    {
        var threshold = GetLowConfidenceThreshold();

        var entity = await BuildRecognitionBaseQuery(asNoTracking: true)
            .FirstOrDefaultAsync(
                r => r.Id == recognitionResultId
                     && ((r.Confidence < threshold) || (r.ExpertId == expertId)),
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException("Recognition result not found for current expert context.");
        }

        return MapDetail(entity);
    }

    public async Task<PagedData<ExpertReviewedRecognitionItemResponse>> GetExpertReviewHistoryAsync(
        Guid expertId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = BuildRecognitionBaseQuery()
            .Where(r => r.ExpertId == expertId
                        && (r.Status == RecognitionStatus.ExpertVerified || r.Status == RecognitionStatus.ExpertRejected));

        return await query
            .OrderByDescending(r => r.ExpertVerifiedAt ?? r.UpdatedAt)
            .Select(r => new ExpertReviewedRecognitionItemResponse
            {
                Media = new Core.Responses.Media.ReportMediaResponse
                {
                    Id = r.ReportMedia.Id,
                    MediaUrl = r.ReportMedia.MediaUrl,
                    FileName = r.ReportMedia.FileName,
                    ContentType = r.ReportMedia.ContentType,
                    FileSize = r.ReportMedia.FileSize,
                    ReferenceType = r.ReportMedia.ReferenceType,
                    Purpose = r.ReportMedia.Purpose,
                    RequiresAIProcessing = r.ReportMedia.RequiresAIProcessing
                },
                AIResult = new Core.Responses.Media.SnakeAIRecognitionResultResponse
                {
                    Id = r.Id,
                    ReportMediaId = r.ReportMediaId,
                    YoloClassName = r.YoloClassName,
                    Confidence = r.Confidence,
                    DetectedSpeciesId = r.DetectedSpeciesId,
                    IsMapped = r.IsMapped,
                    Status = r.Status,
                    DetectedSpecies = r.DetectedSpeciesId.HasValue
                        ? new Core.Responses.SnakeSpecies.SnakeSpeciesResponse
                        {
                            Id = r.DetectedSpecies!.Id,
                            ScientificName = r.DetectedSpecies.ScientificName,
                            Slug = r.DetectedSpecies.Slug,
                            CommonName = r.DetectedSpecies.CommonName,
                            ImageUrl = r.DetectedSpecies.ImageUrl,
                            Description = r.DetectedSpecies.Description,
                            IdentificationSummary = r.DetectedSpecies.IdentificationSummary,
                            PrimaryVenomType = r.DetectedSpecies.GetPrimaryVenomTypeLabel(),
                            RiskLevel = r.DetectedSpecies.RiskLevel,
                            IsVenomous = r.DetectedSpecies.IsVenomous,
                            IsActive = r.DetectedSpecies.IsActive
                        }
                        : null
                },
                ReviewedAt = r.ExpertVerifiedAt
            })
            .ToPaginatedResponse(page, pageSize);
    }

    public async Task<ExpertReviewActionResponse> VerifyRecognitionAsync(
        Guid recognitionResultId,
        Guid expertId,
        ExpertVerifyRecognitionRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureExpertAccountAsync(expertId, cancellationToken);

        var speciesExists = await _unitOfWork.GetRepository<SnakeSpecies>()
            .ExistsAsync(s => s.Id == request.CorrectedSpeciesId && s.IsActive, cancellationToken);

        if (!speciesExists)
        {
            throw new NotFoundException($"Snake species {request.CorrectedSpeciesId} not found.");
        }

        var recognitionResult = await _unitOfWork.GetRepository<SnakeAIRecognitionResult>()
            .FirstOrDefaultAsync(
                predicate: r => r.Id == recognitionResultId,
                asNoTracking: false,
                cancellationToken: cancellationToken);

        if (recognitionResult == null)
        {
            throw new NotFoundException("Recognition result not found.");
        }

        if (recognitionResult.Status != RecognitionStatus.Completed)
        {
            throw new ConflictException("Only recognition results in Completed state can be verified.");
        }

        recognitionResult.Status = RecognitionStatus.ExpertVerified;
        recognitionResult.ExpertId = expertId;
        recognitionResult.ExpertVerifiedAt = DateTime.UtcNow;
        recognitionResult.ExpertCorrectedSpeciesId = request.CorrectedSpeciesId;
        recognitionResult.ExpertNotes = request.ExpertNotes?.Trim();
        recognitionResult.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.GetRepository<SnakeAIRecognitionResult>().Update(recognitionResult);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Expert {ExpertId} verified recognition result {RecognitionResultId} with species {SpeciesId}.",
            expertId,
            recognitionResultId,
            request.CorrectedSpeciesId);

        return new ExpertReviewActionResponse
        {
            RecognitionResultId = recognitionResult.Id,
            Status = recognitionResult.Status,
            ExpertId = recognitionResult.ExpertId,
            ExpertVerifiedAt = recognitionResult.ExpertVerifiedAt,
            ExpertCorrectedSpeciesId = recognitionResult.ExpertCorrectedSpeciesId,
            IsTrainingReady = recognitionResult.Status == RecognitionStatus.ExpertVerified
                              && recognitionResult.ExpertCorrectedSpeciesId.HasValue
        };
    }

    public async Task<ExpertReviewActionResponse> RejectRecognitionAsync(
        Guid recognitionResultId,
        Guid expertId,
        ExpertRejectRecognitionRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureExpertAccountAsync(expertId, cancellationToken);

        var recognitionResult = await _unitOfWork.GetRepository<SnakeAIRecognitionResult>()
            .FirstOrDefaultAsync(
                predicate: r => r.Id == recognitionResultId,
                asNoTracking: false,
                cancellationToken: cancellationToken);

        if (recognitionResult == null)
        {
            throw new NotFoundException("Recognition result not found.");
        }

        if (recognitionResult.Status != RecognitionStatus.Completed)
        {
            throw new ConflictException("Only recognition results in Completed state can be rejected.");
        }

        recognitionResult.Status = RecognitionStatus.ExpertRejected;
        recognitionResult.ExpertId = expertId;
        recognitionResult.ExpertVerifiedAt = DateTime.UtcNow;
        recognitionResult.ExpertCorrectedSpeciesId = null;
        recognitionResult.ExpertNotes = request.ExpertNotes?.Trim();
        recognitionResult.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.GetRepository<SnakeAIRecognitionResult>().Update(recognitionResult);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Expert {ExpertId} rejected recognition result {RecognitionResultId}.",
            expertId,
            recognitionResultId);

        return new ExpertReviewActionResponse
        {
            RecognitionResultId = recognitionResult.Id,
            Status = recognitionResult.Status,
            ExpertId = recognitionResult.ExpertId,
            ExpertVerifiedAt = recognitionResult.ExpertVerifiedAt,
            ExpertCorrectedSpeciesId = recognitionResult.ExpertCorrectedSpeciesId,
            IsTrainingReady = false
        };
    }

    public async Task<PagedData<AIRecognitionAdminReportMediaListItemResponse>> GetAdminReportMediaListAsync(
        int page,
        int pageSize,
        RecognitionStatus? status,
        decimal? minConfidence,
        decimal? maxConfidence,
        MediaReferenceType? referenceType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var threshold = GetLowConfidenceThreshold();

        var query = BuildRecognitionBaseQuery();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        query = ApplyCommonFilters(query, minConfidence, maxConfidence, referenceType, from, to);

        return await ProjectAdminListItems(query, threshold)
            .OrderByDescending(r => r.CreatedAt)
            .ToPaginatedResponse(page, pageSize);
    }

    public async Task<AIRecognitionAdminReportMediaListItemResponse> GetAdminReportMediaDetailAsync(
        Guid recognitionResultId,
        CancellationToken cancellationToken = default)
    {
        var threshold = GetLowConfidenceThreshold();

        var detail = await ProjectAdminListItems(BuildRecognitionBaseQuery(asNoTracking: true), threshold)
            .FirstOrDefaultAsync(r => r.RecognitionResultId == recognitionResultId, cancellationToken);

        if (detail == null)
        {
            throw new NotFoundException("AI recognition report media item not found.");
        }

        return detail;
    }

    private IQueryable<SnakeAIRecognitionResult> BuildRecognitionBaseQuery(bool asNoTracking = true)
    {
        return _unitOfWork.GetRepository<SnakeAIRecognitionResult>()
            .CreateBaseQuery(asNoTracking)
            .Include(r => r.ReportMedia)
            .Include(r => r.DetectedSpecies)
            .Include(r => r.ExpertCorrectedSpecies)
            .Include(r => r.Expert);
    }

    private static IQueryable<SnakeAIRecognitionResult> ApplyCommonFilters(
        IQueryable<SnakeAIRecognitionResult> query,
        decimal? minConfidence,
        decimal? maxConfidence,
        MediaReferenceType? referenceType,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (minConfidence.HasValue)
        {
            query = query.Where(r => r.Confidence >= minConfidence.Value);
        }

        if (maxConfidence.HasValue)
        {
            query = query.Where(r => r.Confidence <= maxConfidence.Value);
        }

        if (referenceType.HasValue)
        {
            query = query.Where(r => r.ReportMedia.ReferenceType == referenceType.Value);
        }

        if (from.HasValue)
        {
            var fromUtc = from.Value.UtcDateTime;
            query = query.Where(r => r.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = to.Value.UtcDateTime;
            query = query.Where(r => r.CreatedAt <= toUtc);
        }

        return query;
    }

    private static IQueryable<AIRecognitionAdminReportMediaListItemResponse> ProjectAdminListItems(
        IQueryable<SnakeAIRecognitionResult> query,
        decimal threshold)
    {
        return query.Select(r => new AIRecognitionAdminReportMediaListItemResponse
        {
            RecognitionResultId = r.Id,
            ReportMediaId = r.ReportMediaId,
            MediaUrl = r.ReportMedia.MediaUrl,
            ContentType = r.ReportMedia.ContentType,
            ReferenceId = r.ReportMedia.ReferenceId,
            ReferenceType = r.ReportMedia.ReferenceType,
            Purpose = r.ReportMedia.Purpose,
            AIModelId = r.AIModelId,
            YoloClassName = r.YoloClassName,
            Confidence = r.Confidence,
            DetectedSpecies = r.DetectedSpeciesId.HasValue
                ? new AIRecognitionSpeciesLiteResponse
                {
                    Id = r.DetectedSpecies!.Id,
                    CommonName = r.DetectedSpecies.CommonName
                }
                : null,
            ExpertCorrectedSpecies = r.ExpertCorrectedSpeciesId.HasValue
                ? new AIRecognitionSpeciesLiteResponse
                {
                    Id = r.ExpertCorrectedSpecies!.Id,
                    CommonName = r.ExpertCorrectedSpecies.CommonName
                }
                : null,
            ExpertReviewerName = r.Expert != null ? r.Expert.FullName : null,
            Status = r.Status,
            NeedsExpertReview = r.Status == RecognitionStatus.Completed && r.Confidence < threshold,
            ExpertNotes = r.ExpertNotes,
            ExpertVerifiedAt = r.ExpertVerifiedAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        });
    }

    private AIRecognitionReviewDetailResponse MapDetail(SnakeAIRecognitionResult entity)
    {
        return new AIRecognitionReviewDetailResponse
        {
            RecognitionResultId = entity.Id,
            Media = new Core.Responses.Media.ReportMediaResponse
            {
                Id = entity.ReportMediaId,
                MediaUrl = entity.ReportMedia.MediaUrl,
                ContentType = entity.ReportMedia.ContentType,
                FileName = entity.ReportMedia.FileName,
                FileSize = entity.ReportMedia.FileSize,
                Purpose = entity.ReportMedia.Purpose,
                ReferenceType = entity.ReportMedia.ReferenceType,
                RequiresAIProcessing = entity.ReportMedia.RequiresAIProcessing
            },
            AIResult = new Core.Responses.Media.SnakeAIRecognitionResultResponse
            {
                Id = entity.Id,
                ReportMediaId = entity.ReportMediaId,
                YoloClassName = entity.YoloClassName,
                Confidence = entity.Confidence,
                DetectedSpeciesId = entity.DetectedSpeciesId,
                IsMapped = entity.IsMapped,
                Status = entity.Status,
                DetectedSpecies = entity.DetectedSpeciesId.HasValue && entity.DetectedSpecies != null
                    ? new Core.Responses.SnakeSpecies.SnakeSpeciesResponse
                    {
                        Id = entity.DetectedSpecies.Id,
                        ScientificName = entity.DetectedSpecies.ScientificName,
                        Slug = entity.DetectedSpecies.Slug,
                        CommonName = entity.DetectedSpecies.CommonName,
                        ImageUrl = entity.DetectedSpecies.ImageUrl,
                        Description = entity.DetectedSpecies.Description,
                        IdentificationSummary = entity.DetectedSpecies.IdentificationSummary,
                        PrimaryVenomType = entity.DetectedSpecies.GetPrimaryVenomTypeLabel(),
                        RiskLevel = entity.DetectedSpecies.RiskLevel,
                        IsVenomous = entity.DetectedSpecies.IsVenomous,
                        IsActive = entity.DetectedSpecies.IsActive
                    }
                    : null
            }
        };
    }

    private decimal GetLowConfidenceThreshold()
    {
        var threshold = _systemSettingService.GetSetting(SystemSettingKeys.AIRecognitionLowConfidenceThreshold, DefaultLowConfidenceThreshold);

        if (threshold < 0 || threshold > 1)
        {
            _logger.LogWarning("Configured low confidence threshold {Threshold} is out of range [0,1]. Falling back to default {Default}.",
                threshold,
                DefaultLowConfidenceThreshold);
            return DefaultLowConfidenceThreshold;
        }

        return threshold;
    }

    private async Task EnsureExpertAccountAsync(Guid expertId, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
            predicate: a => a.Id == expertId,
            include: q => q.Include(a => a.ExpertProfile),
            cancellationToken: cancellationToken);

        if (account == null || account.Role != AccountRole.Expert || account.ExpertProfile == null)
        {
            throw new ForbiddenException("Current user is not a valid expert account.");
        }
    }
}
