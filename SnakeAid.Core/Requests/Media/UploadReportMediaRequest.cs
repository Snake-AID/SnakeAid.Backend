using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SnakeAid.Core.Requests.Media;

/// <summary>
/// Request to upload media for reports (Community Report, Snakebite Incident, etc.)
/// </summary>
public class UploadReportMediaRequest
{
    /// <summary>
    /// The image/video file to upload
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = default!;

    /// <summary>
    /// ID of the parent entity (IncidentId, ReportId, etc.)
    /// </summary>
    [Required]
    public Guid ReferenceId { get; set; }
}
