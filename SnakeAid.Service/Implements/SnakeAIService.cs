using Microsoft.Extensions.Logging;
using SnakeAid.Core.Requests.AIVision;
using SnakeAid.Core.Responses.AIVision;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

/// <summary>
/// Service implementation for SnakeAI integration
/// </summary>
public class SnakeAIService : ISnakeAIService
{
    private readonly ISnakeAIApi _api;
    private readonly ILogger<SnakeAIService> _logger;

    public SnakeAIService(ISnakeAIApi api, ILogger<SnakeAIService> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SnakeAIDetectResponse> DetectAsync(string imageUrl, float confidence = 0.25f)
    {
        var request = new SnakeAIDetectRequest
        {
            ImageUrl = imageUrl,
            Confidence = confidence
        };

        _logger.LogInformation("Calling SnakeAI detect for URL: {Url} with confidence: {Confidence}",
            imageUrl, confidence);

        try
        {
            var response = await _api.DetectByUrlAsync(request);

            _logger.LogInformation(
                "SnakeAI detected {Count} objects. Top: {TopClass} ({TopConfidence:P0})",
                response.Detections.Count,
                response.Detections.FirstOrDefault()?.ClassName ?? "none",
                response.Detections.FirstOrDefault()?.Confidence ?? 0);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SnakeAI detection failed for URL: {Url}", imageUrl);
            throw;
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
