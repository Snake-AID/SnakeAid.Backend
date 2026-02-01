using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.SnakeDetection;

namespace SnakeAid.Service.Interfaces;

/// <summary>
/// Service interface for SnakeAI integration
/// </summary>
public interface ISnakeAIService
{
    /// <summary>
    /// Detect snake from image URL. Confidence threshold is taken from configured SnakeAI settings.
    /// </summary>
    /// <param name="imageUrl">Public URL of the image (Cloudinary)</param>
    /// <returns>API response with detection results</returns>
    Task<ApiResponse<SnakeDetectionResponse>> DetectAsync(string imageUrl);

    /// <summary>
    /// Check if SnakeAI service is healthy (internal use)
    /// </summary>
    /// <returns>True if service is up and model is loaded</returns>
    Task<bool> IsHealthyAsync();
}
