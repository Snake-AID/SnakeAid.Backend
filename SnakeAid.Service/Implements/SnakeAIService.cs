using System.Net;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SnakeAI;
using SnakeAid.Core.Responses.SnakeDetection;
using SnakeAid.Core.Settings;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

/// <summary>
/// Service implementation for SnakeAI integration
/// </summary>
public class SnakeAIService : ISnakeAIService
{
    private readonly ISnakeAIApi _api;
    private readonly ILogger<SnakeAIService> _logger;
    private readonly SnakeAISettings _settings;

    public SnakeAIService(ISnakeAIApi api, ILogger<SnakeAIService> logger, SnakeAISettings settings)
    {
        _api = api;
        _logger = logger;
        _settings = settings;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<SnakeDetectionResponse>> DetectAsync(string imageUrl)
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

        _logger.LogInformation("Calling SnakeAI detect for URL: {Url} with confidence: {Confidence}",
            imageUrl, _settings.Confidence);

        try
        {
            var result = await _api.DetectByUrlAsync(request);

            var topDetection = result.Detections
                .OrderByDescending(d => d.Confidence)
                .FirstOrDefault();

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
                Detections = result.Detections.Select(d => new Core.Responses.SnakeDetection.SnakeAIDetection
                {
                    ClassId = d.ClassId,
                    ClassName = d.ClassName,
                    Confidence = d.Confidence,
                    X = d.Bbox.X1,
                    Y = d.Bbox.Y1,
                    Width = d.Bbox.X2 - d.Bbox.X1,
                    Height = d.Bbox.Y2 - d.Bbox.Y1
                }).ToList(),
                Warnings = result.Warnings != null ? new Core.Responses.SnakeDetection.SnakeAIWarnings
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
            _logger.LogError(ex, "SnakeAI detection failed for URL: {Url}", imageUrl);
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
}
