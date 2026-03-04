using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.SnakeAI;
using SnakeAid.Core.Responses.SnakeDetection;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

/// <summary>
/// Service implementation for SnakeAI integration with species mapping and persistence
/// </summary>
public class SnakeAIService : ISnakeAIService
{
    private readonly ISnakeAIApi _api;
    private readonly ILogger<SnakeAIService> _logger;
    private readonly SnakeAISettings _settings;
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

    public SnakeAIService(
        ISnakeAIApi api,
        ILogger<SnakeAIService> logger,
        SnakeAISettings settings,
        IUnitOfWork<SnakeAidDbContext> unitOfWork)
    {
        _api = api;
        _logger = logger;
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<SnakeDetectionResponse> DetectAsync(string imageUrl, Guid reportMediaId, CancellationToken ct = default)
    {
        var request = new SnakeAIDetectRequest
        {
            ImageUrl = imageUrl,
            Confidence = _settings.Confidence,
            ImageSize = _settings.ImageSize,
            Iou = _settings.IouThreshold,
            TopK = _settings.TopK,
            SaveImage = _settings.SaveImage
        };

        _logger.LogInformation("Calling SnakeAI detect for ReportMediaId: {MediaId}, URL: {Url} with confidence: {Confidence}",
            reportMediaId, imageUrl, _settings.Confidence);

        try
        {
            // 1. Call AI Service
            var result = await _api.DetectByUrlAsync(request);

            // 2. Get Active AIModel from database
            var activeModel = await _unitOfWork.GetRepository<AIModel>()
                .FirstOrDefaultAsync(
                    predicate: m => m.IsActive && m.IsDefault,
                    cancellationToken: ct);

            if (activeModel == null)
            {
                _logger.LogWarning("No active/default AI model found in database");
                // Continue without mapping - but log warning
            }

            // 3. Process detections and enrich with species info
            var results = new List<DetectionResult>();
            SnakeAIRecognitionResult? savedRecognitionResult = null;

            var topDetection = result.Detections
                .OrderByDescending(d => d.Confidence)
                .FirstOrDefault();

            foreach (var detection in result.Detections)
            {
                var detectionResult = new DetectionResult
                {
                    Ai = new AiDetection
                    {
                        ClassId = detection.ClassId,
                        ClassName = detection.ClassName,
                        Confidence = detection.Confidence,
                        BBox = new SnakeBBox
                        {
                            X1 = detection.Bbox.X1,
                            Y1 = detection.Bbox.Y1,
                            X2 = detection.Bbox.X2,
                            Y2 = detection.Bbox.Y2
                        }
                    }
                };

                // 4. Map to SnakeSpecies if model exists
                if (activeModel != null)
                {
                    var mapping = await _unitOfWork.GetRepository<AISnakeClassMapping>()
                        .FirstOrDefaultAsync(
                            predicate: m => m.AIModelId == activeModel.Id && m.YoloClassName == detection.ClassName && m.IsActive,
                            include: q => q
                                .Include(m => m.SnakeSpecies)
                                    .ThenInclude(s => s.SpeciesVenoms)
                                        .ThenInclude(sv => sv.VenomType)
                                            .ThenInclude(v => v.FirstAidGuideline), // Include for Fallback
                            cancellationToken: ct);

                    if (mapping?.SnakeSpecies != null)
                    {
                        var species = mapping.SnakeSpecies;

                        // -- FALLBACK LOGIC FOR FIRST AID --
                        // Check override first, if null then fallback to VenomType's guide
                        if (species.FirstAidGuidelineOverride == null && species.SpeciesVenoms.Any())
                        {
                            // Try to find any FirstAidGuideline from linked VenomTypes
                            var venomWithGuide = species.SpeciesVenoms
                                .Select(sv => sv.VenomType)
                                .FirstOrDefault(v => v.FirstAidGuideline != null);

                            if (venomWithGuide != null)
                            {
                                species.FirstAidGuidelineOverride = new FirstAidOverride
                                {
                                    Mode = OverrideMode.Append, // Append mode (0)
                                    Content = venomWithGuide.FirstAidGuideline.Content ?? new FirstAidContent()
                                };
                            }
                        }

                        detectionResult.Snake = species;

                        _logger.LogInformation(
                            "Mapped YOLO class '{YoloClass}' to species '{Species}' (ID: {SpeciesId}, Venomous: {IsVenomous})",
                            detection.ClassName, species.CommonName, species.Id, species.IsVenomous);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "No mapping found for YOLO class '{YoloClass}' with AIModel ID: {ModelId}",
                            detection.ClassName, activeModel.Id);
                    }
                }

                results.Add(detectionResult);
            }

            // 5. Save Recognition Result for top detection
            if (topDetection != null && activeModel != null)
            {
                var topEnriched = results.FirstOrDefault(r => r.Ai.ClassName == topDetection.ClassName && r.Ai.Confidence == topDetection.Confidence);

                var recognitionResult = new SnakeAIRecognitionResult
                {
                    Id = Guid.NewGuid(),
                    ReportMediaId = reportMediaId,
                    AIModelId = activeModel.Id,
                    YoloClassName = topDetection.ClassName,
                    Confidence = (decimal)topDetection.Confidence,
                    DetectedSpeciesId = topEnriched?.Snake?.Id,
                    IsMapped = topEnriched?.Snake != null,
                    AllDetections = JsonSerializer.Serialize(result.Detections),
                    Status = RecognitionStatus.Completed
                };

                await _unitOfWork.GetRepository<SnakeAIRecognitionResult>().InsertAsync(recognitionResult, ct);

                // Update ReportMedia processing status
                // Use the tracked entity from context instead of querying again to avoid tracking conflicts
                var reportMediaEntry = _unitOfWork.Context.ChangeTracker.Entries<ReportMedia>()
                    .FirstOrDefault(e => e.Entity.Id == reportMediaId);

                ReportMedia? reportMedia = null;
                if (reportMediaEntry != null)
                {
                    // Entity already tracked, use it
                    reportMedia = reportMediaEntry.Entity;
                }
                else
                {
                    // Entity not tracked, query it
                    reportMedia = await _unitOfWork.GetRepository<ReportMedia>()
                        .FirstOrDefaultAsync(predicate: m => m.Id == reportMediaId, asNoTracking: false, cancellationToken: ct);
                }

                if (reportMedia != null)
                {
                    reportMedia.IsProcessed = true;
                    reportMedia.ProcessedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<ReportMedia>().Update(reportMedia);
                }

                // Don't commit here - let the calling service handle transaction commit
                // await _unitOfWork.CommitAsync();
                savedRecognitionResult = recognitionResult;

                _logger.LogInformation(
                    "Saved recognition result {ResultId} for ReportMedia {MediaId}. Mapped: {IsMapped}",
                    recognitionResult.Id, reportMediaId, recognitionResult.IsMapped);
            }

            _logger.LogInformation(
                "SnakeAI detected {Count} objects. Top: {TopClass} ({TopConfidence:P0})",
                result.Detections.Count,
                topDetection?.ClassName ?? "none",
                topDetection?.Confidence ?? 0);

            return new SnakeDetectionResponse
            {
                Metadata = new AiMetadata
                {
                    ModelVersion = result.ModelVersion,
                    ImageWidth = result.ImageWidth,
                    ImageHeight = result.ImageHeight,
                    DetectionCount = result.Detections.Count,
                    Warnings = result.Warnings != null ? new SnakeAIWarnings
                    {
                        Blur = result.Warnings.Blur,
                        Brightness = result.Warnings.Brightness,
                        TooSmall = result.Warnings.TooSmall
                    } : null
                },
                Results = results,
                RecognitionResultId = savedRecognitionResult?.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SnakeAI detection failed for ReportMediaId: {MediaId}, URL: {Url}", reportMediaId, imageUrl);

            // Save failed recognition result
            try
            {
                var activeModel = await _unitOfWork.GetRepository<AIModel>()
                    .FirstOrDefaultAsync(predicate: m => m.IsActive && m.IsDefault, cancellationToken: ct);

                if (activeModel != null)
                {
                    var failedResult = new SnakeAIRecognitionResult
                    {
                        Id = Guid.NewGuid(),
                        ReportMediaId = reportMediaId,
                        AIModelId = activeModel.Id,
                        YoloClassName = "ERROR",
                        Confidence = 0,
                        IsMapped = false,
                        Status = RecognitionStatus.Failed
                    };

                    await _unitOfWork.GetRepository<SnakeAIRecognitionResult>().InsertAsync(failedResult, ct);
                    await _unitOfWork.CommitAsync();
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save error recognition result for ReportMediaId: {MediaId}", reportMediaId);
            }

            throw new ApiException("Snake detection failed. Please try again later.", System.Net.HttpStatusCode.InternalServerError);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var health = await _api.HealthCheckAsync();
            var isHealthy = health.Status == "ok" && health.ModelLoaded;

            if (!isHealthy)
            {
                _logger.LogWarning(
                    "SnakeAI unhealthy. Status: {Status}, ModelLoaded: {ModelLoaded}",
                    health.Status, health.ModelLoaded);
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SnakeAI health check failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<SnakeDetectionResponse> DetectFromReportMediaAsync(Guid reportMediaId, CancellationToken ct = default)
    {
        // 1. Health check
        if (!await IsHealthyAsync())
        {
            _logger.LogWarning("SnakeAI server is unavailable");
            throw new ApiException("Snake detection server is currently unavailable. Please try again later.",
                System.Net.HttpStatusCode.ServiceUnavailable);
        }

        // 2. Validate ReportMedia exists
        var reportMedia = await _unitOfWork.GetRepository<ReportMedia>()
            .FirstOrDefaultAsync(
                predicate: m => m.Id == reportMediaId,
                cancellationToken: ct);

        if (reportMedia == null)
        {
            _logger.LogWarning("ReportMedia not found: {MediaId}", reportMediaId);
            throw new NotFoundException("ReportMedia not found.");
        }

        // 3. Call detection with imageUrl
        return await DetectAsync(reportMedia.MediaUrl, reportMediaId, ct);
    }

    /// <inheritdoc />
    public async Task<SnakeDetectionResponse> GetRecognitionResultAsync(Guid recognitionResultId, CancellationToken ct = default)
    {
        var recognitionResult = await _unitOfWork.GetRepository<SnakeAIRecognitionResult>()
            .FirstOrDefaultAsync(
                predicate: r => r.Id == recognitionResultId,
                include: query => query
                    .Include(r => r.ReportMedia)
                    .Include(r => r.AIModel)
                    .Include(r => r.DetectedSpecies)
                        .ThenInclude(s => s.SpeciesVenoms)
                            .ThenInclude(sv => sv.VenomType)
                                .ThenInclude(v => v.FirstAidGuideline), // Include for Fallback
                cancellationToken: ct);

        if (recognitionResult == null)
        {
            _logger.LogWarning("Recognition result not found: {ResultId}", recognitionResultId);
            throw new NotFoundException("Recognition result not found.");
        }

        // Restore species logic if available
        if (recognitionResult.DetectedSpecies != null)
        {
            var species = recognitionResult.DetectedSpecies;
            // -- FALLBACK LOGIC --
            if (species.FirstAidGuidelineOverride == null && species.SpeciesVenoms.Any())
            {
                var venomWithGuide = species.SpeciesVenoms
                     .Select(sv => sv.VenomType)
                     .FirstOrDefault(v => v.FirstAidGuideline != null);

                if (venomWithGuide != null)
                {
                    species.FirstAidGuidelineOverride = new FirstAidOverride
                    {
                        Mode = OverrideMode.Append,
                        Content = venomWithGuide.FirstAidGuideline.Content ?? new FirstAidContent()
                    };
                }
            }
        }

        _logger.LogInformation("Retrieved recognition result: {ResultId}", recognitionResultId);

        // Build response from saved data (matching V3 strict structure)
        return new SnakeDetectionResponse
        {
            Metadata = new AiMetadata
            {
                ModelVersion = recognitionResult.AIModel?.Version,
                ImageWidth = 0, // Not stored
                ImageHeight = 0,
                DetectionCount = 1,
                Warnings = null
            },
            RecognitionResultId = recognitionResult.Id,
            Results = new List<DetectionResult>
            {
                new DetectionResult
                {
                    Ai = new AiDetection
                    {
                        ClassId = 0,
                        ClassName = recognitionResult.YoloClassName,
                        Confidence = (float)recognitionResult.Confidence,
                        BBox = new SnakeBBox() // BBox data not stored efficiently yet
                    },
                    Snake = recognitionResult.DetectedSpecies
                }
            }
        };
    }
}
