using SnakeAid.Core.Responses.AIVision;

namespace SnakeAid.Service.Interfaces;

/// <summary>
/// Service interface for SnakeAI integration
/// </summary>
public interface ISnakeAIService
{
    /// <summary>
    /// Detect snake from image URL
    /// </summary>
    /// <param name="imageUrl">Public URL of the image (Cloudinary)</param>
    /// <param name="confidence">Confidence threshold (default: 0.25)</param>
    /// <returns>Detection response with results</returns>
    Task<SnakeAIDetectResponse> DetectAsync(string imageUrl, float confidence = 0.25f);

    /// <summary>
    /// Check if SnakeAI service is healthy (internal use)
    /// </summary>
    /// <returns>True if service is up and model is loaded</returns>
    Task<bool> IsHealthyAsync();
}
