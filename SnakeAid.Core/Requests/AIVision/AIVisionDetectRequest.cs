namespace SnakeAid.Core.Requests.AIVision;

/// <summary>
/// Request to detect snake from image URL
/// </summary>
public class AIVisionDetectRequest
{
    /// <summary>
    /// Public URL of the image (preferably Cloudinary)
    /// </summary>
    public required string ImageUrl { get; set; }

    /// <summary>
    /// Confidence threshold (0.0-1.0), default: 0.25
    /// </summary>
    public float Confidence { get; set; } = 0.25f;
}
