using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
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
    public async Task<ApiResponse<SnakeDetectionResponse>> DetectAsync(string imageUrl, Guid reportMediaId, CancellationToken ct = default)
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
            var enrichedDetections = new List<SnakeAIDetection>();
            SnakeAIRecognitionResult? savedRecognitionResult = null;

            var topDetection = result.Detections
                .OrderByDescending(d => d.Confidence)
                .FirstOrDefault();

            foreach (var detection in result.Detections)
            {
                var enriched = new SnakeAIDetection
                {
                    ClassId = detection.ClassId,
                    ClassName = detection.ClassName,
                    Confidence = detection.Confidence,
                    X = detection.Bbox.X1,
                    Y = detection.Bbox.Y1,
                    Width = detection.Bbox.X2 - detection.Bbox.X1,
                    Height = detection.Bbox.Y2 - detection.Bbox.Y1
                };

                // 4. Map to SnakeSpecies if model exists
                if (activeModel != null)
                {
                    var mapping = await _unitOfWork.GetRepository<AISnakeClassMapping>()
                        .FirstOrDefaultAsync(
                            predicate: m => m.AIModelId == activeModel.Id && m.YoloClassName == detection.ClassName && m.IsActive,
                            include: q => q.Include(m => m.SnakeSpecies),
                            cancellationToken: ct);

                    if (mapping?.SnakeSpecies != null)
                    {
                        enriched.SpeciesId = mapping.SnakeSpecies.Id;
                        enriched.SpeciesName = mapping.SnakeSpecies.CommonName;
                        enriched.ScientificName = mapping.SnakeSpecies.ScientificName;
                        enriched.IsVenomous = mapping.SnakeSpecies.IsVenomous;
                        enriched.RiskLevel = mapping.SnakeSpecies.RiskLevel;

                        _logger.LogInformation(
                            "Mapped YOLO class '{YoloClass}' to species '{Species}' (ID: {SpeciesId}, Venomous: {IsVenomous})",
                            detection.ClassName, mapping.SnakeSpecies.CommonName, mapping.SnakeSpecies.Id, mapping.SnakeSpecies.IsVenomous);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "No mapping found for YOLO class '{YoloClass}' with AIModel ID: {ModelId}",
                            detection.ClassName, activeModel.Id);
                    }
                }

                enrichedDetections.Add(enriched);
            }

            // 5. Save Recognition Result for top detection
            if (topDetection != null && activeModel != null)
            {
                var topEnriched = enrichedDetections.FirstOrDefault(e => e.ClassName == topDetection.ClassName && e.Confidence == topDetection.Confidence);

                var recognitionResult = new SnakeAIRecognitionResult
                {
                    Id = Guid.NewGuid(),
                    ReportMediaId = reportMediaId,
                    AIModelId = activeModel.Id,
                    YoloClassName = topDetection.ClassName,
                    Confidence = (decimal)topDetection.Confidence,
                    DetectedSpeciesId = topEnriched?.SpeciesId,
                    IsMapped = topEnriched?.SpeciesId.HasValue ?? false,
                    AllDetections = JsonSerializer.Serialize(result.Detections),
                    Status = RecognitionStatus.Completed
                };

                await _unitOfWork.GetRepository<SnakeAIRecognitionResult>().InsertAsync(recognitionResult, ct);
                
                // Update ReportMedia processing status
                var reportMedia = await _unitOfWork.GetRepository<ReportMedia>()
                    .FirstOrDefaultAsync(predicate: m => m.Id == reportMediaId, asNoTracking: false, cancellationToken: ct);
                
                if (reportMedia != null)
                {
                    reportMedia.IsProcessed = true;
                    reportMedia.ProcessedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<ReportMedia>().Update(reportMedia);
                }

                await _unitOfWork.CommitAsync();
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

            var response = new SnakeDetectionResponse
            {
                ModelVersion = result.ModelVersion,
                ImageWidth = result.ImageWidth,
                ImageHeight = result.ImageHeight,
                TopClassName = topDetection?.ClassName,
                TopConfidence = topDetection?.Confidence,
                DetectionCount = result.Detections.Count,
                Detections = enrichedDetections,
                RecognitionResultId = savedRecognitionResult?.Id,
                Warnings = result.Warnings != null ? new SnakeAIWarnings
                {
                    Blur = result.Warnings.Blur,
                    Brightness = result.Warnings.Brightness,
                    TooSmall = result.Warnings.TooSmall
                } : null
            };

            return ApiResponseBuilder.BuildSuccessResponse(response, "Snake detection completed successfully.");
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

            return ApiResponseBuilder.CreateResponse<SnakeDetectionResponse>(
                null,
                false,
                "Snake detection failed. Please try again later.",
                HttpStatusCode.InternalServerError,
                "DETECTION_FAILED");
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
    public async Task<ApiResponse<SnakeDetectionResponse>> DetectFromReportMediaAsync(Guid reportMediaId, CancellationToken ct = default)
    {
        try
        {
            // 1. Health check
            if (!await IsHealthyAsync())
            {
                _logger.LogWarning("SnakeAI service is unavailable");
                return ApiResponseBuilder.CreateResponse<SnakeDetectionResponse>(
                    null, false, "Snake detection service is currently unavailable. Please try again later.",
                    System.Net.HttpStatusCode.ServiceUnavailable, "SERVICE_UNAVAILABLE");
            }

            // 2. Validate ReportMedia exists
            var reportMedia = await _unitOfWork.GetRepository<ReportMedia>()
                .FirstOrDefaultAsync(
                    predicate: m => m.Id == reportMediaId,
                    cancellationToken: ct);

            if (reportMedia == null)
            {
                _logger.LogWarning("ReportMedia not found: {MediaId}", reportMediaId);
                return ApiResponseBuilder.CreateResponse<SnakeDetectionResponse>(
                    null, false, "ReportMedia not found.",
                    System.Net.HttpStatusCode.NotFound, "NOT_FOUND");
            }

            // 3. Call detection with imageUrl
            return await DetectAsync(reportMedia.MediaUrl, reportMediaId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DetectFromReportMediaAsync for ReportMediaId: {MediaId}", reportMediaId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<SnakeDetectionResponse>> GetRecognitionResultAsync(Guid recognitionResultId, CancellationToken ct = default)
    {
        try
        {
            var recognitionResult = await _unitOfWork.GetRepository<SnakeAIRecognitionResult>()
                .FirstOrDefaultAsync(
                    predicate: r => r.Id == recognitionResultId,
                    include: query => query
                        .Include(r => r.ReportMedia)
                        .Include(r => r.AIModel)
                        .Include(r => r.DetectedSpecies),
                    cancellationToken: ct);

            if (recognitionResult == null)
            {
                _logger.LogWarning("Recognition result not found: {ResultId}", recognitionResultId);
                return ApiResponseBuilder.CreateResponse<SnakeDetectionResponse>(
                    null, false, "Recognition result not found.",
                    System.Net.HttpStatusCode.NotFound, "NOT_FOUND");
            }

            // Build response from saved data (matching current SnakeDetectionResponse structure)
            var response = new SnakeDetectionResponse
            {
                ModelVersion = recognitionResult.AIModel?.Version,
                ImageWidth = 0, // These aren't stored, set defaults
                ImageHeight = 0,
                TopClassName = recognitionResult.YoloClassName,
                TopConfidence = (float)recognitionResult.Confidence,
                DetectionCount = 1,
                RecognitionResultId = recognitionResult.Id,
                Detections = new List<SnakeAIDetection>
                {
                    new SnakeAIDetection
                    {
                        ClassId = 0, // Not stored in entity, use default
                        ClassName = recognitionResult.YoloClassName,
                        Confidence = (float)recognitionResult.Confidence,
                        X = 0, // BoundingBox info not stored in simple format, use defaults
                        Y = 0,
                        Width = 0,
                        Height = 0,
                        SpeciesId = recognitionResult.DetectedSpeciesId,
                        SpeciesName = recognitionResult.DetectedSpecies?.CommonName,
                        ScientificName = recognitionResult.DetectedSpecies?.ScientificName,
                        IsVenomous = recognitionResult.DetectedSpecies?.IsVenomous,
                        RiskLevel = recognitionResult.DetectedSpecies?.RiskLevel
                    }
                }
            };

            _logger.LogInformation("Retrieved recognition result: {ResultId}", recognitionResultId);
            return ApiResponseBuilder.BuildSuccessResponse(response, "Recognition result retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recognition result: {ResultId}", recognitionResultId);
            throw;
        }
    }
}
