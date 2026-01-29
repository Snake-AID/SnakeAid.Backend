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
} 
