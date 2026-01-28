namespace SnakeAid.Core.Responses.Media;

public class CloudinaryUploadResult
{
    public string SecureUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Format { get; set; }
    public long? Bytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string Folder { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; set; } = Array.Empty<string>();
}

