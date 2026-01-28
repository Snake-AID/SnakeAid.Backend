using Refit;
using SnakeAid.Core.Requests.AIVision;
using SnakeAid.Core.Responses.AIVision;

namespace SnakeAid.Service.Interfaces;

/// <summary>
/// Refit interface for SnakeAI FastAPI service
/// </summary>
public interface ISnakeAIApi
{
    /// <summary>
    /// Detect snake from image URL
    /// </summary>
    [Post("/detect/url")]
    Task<SnakeAIDetectResponse> DetectByUrlAsync([Body] SnakeAIDetectRequest request);

    /// <summary>
    /// Check service health (internal use only)
    /// </summary>
    [Get("/health")]
    Task<SnakeAIHealthResponse> HealthCheckAsync();
}
