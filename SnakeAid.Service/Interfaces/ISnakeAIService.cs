using SnakeAid.Core.Responses.SnakeDetection;

namespace SnakeAid.Service.Interfaces;

/// <summary>
/// Service interface for SnakeAI integration
/// </summary>
public interface ISnakeAIService
{
    /// <summary>
    /// Detect snake from ReportMedia, map to species, and persist result.
    /// </summary>
    /// <param name="reportMediaId">ID of the ReportMedia entity</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detection results including species info</returns>
    Task<SnakeDetectionResponse> DetectFromReportMediaAsync(Guid reportMediaId, CancellationToken ct = default);

    /// <summary>
    /// Get saved recognition result by ID
    /// </summary>
    /// <param name="recognitionResultId">ID of the SnakeAIRecognitionResult</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Recognition result details</returns>
    Task<SnakeDetectionResponse> GetRecognitionResultAsync(Guid recognitionResultId, CancellationToken ct = default);

    /// <summary>
    /// Detect snake from ReportMedia, map to species, and persist result.
    /// </summary>
    /// <param name="imageUrl">Public URL of the image (Cloudinary)</param>
    /// <param name="reportMediaId">ID of the ReportMedia entity</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detection results including species info</returns>
    Task<SnakeDetectionResponse> DetectAsync(string imageUrl, Guid reportMediaId, CancellationToken ct = default);

    /// <summary>
    /// Check if SnakeAI service is healthy (internal use)
    /// </summary>
    /// <returns>True if service is up and model is loaded</returns>
    Task<bool> IsHealthyAsync();
}
