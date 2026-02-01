using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.Media;

/// <summary>
/// Response for uploaded report media
/// </summary>
public class ReportMediaResponse
{
    /// <summary>
    /// Unique identifier of the media record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Public URL of the uploaded media (Cloudinary)
    /// </summary>
    public string MediaUrl { get; set; } = string.Empty;

    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Reference type (CommunityReport, SnakebiteIncident, etc.)
    /// </summary>
    public MediaReferenceType ReferenceType { get; set; }

    /// <summary>
    /// Media purpose (SnakeIdentification, Evidence, etc.)
    /// </summary>
    public MediaPurpose Purpose { get; set; }

    /// <summary>
    /// Whether this media requires AI processing
    /// </summary>
    public bool RequiresAIProcessing { get; set; }
}
