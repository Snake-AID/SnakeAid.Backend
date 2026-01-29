using System.Text.Json.Serialization;

namespace SnakeAid.Core.Requests.SnakeDetection;

/// <summary>
/// Request to detect snake from image URL
/// </summary>
public class SnakeDetectionRequest
{
    /// <summary>
    /// Public URL of the image (preferably Cloudinary)
    /// </summary>
    public required string ImageUrl { get; set; }
}
