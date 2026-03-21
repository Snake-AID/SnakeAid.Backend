using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.LibraryMedia;

public class LibraryMediaResponse
{
    public Guid Id { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public string? FileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }
    public Guid? UploadedById { get; set; }
    public DateTime? UploadedAt { get; set; }
    public int? SnakeSpeciesId { get; set; }
}