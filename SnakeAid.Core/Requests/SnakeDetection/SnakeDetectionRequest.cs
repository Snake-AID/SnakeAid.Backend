using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.SnakeDetection;

/// <summary>
/// Request to detect snake from uploaded ReportMedia
/// </summary>
public class SnakeDetectionRequest
{
    /// <summary>
    /// ID of the ReportMedia entity containing the image
    /// </summary>
    [Required]
    public Guid ReportMediaId { get; set; }
}
