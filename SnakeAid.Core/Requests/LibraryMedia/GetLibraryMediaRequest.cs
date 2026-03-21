using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;

namespace SnakeAid.Core.Requests.LibraryMedia;

public class GetLibraryMediaRequest : PaginationRequest
{
    public int? SnakeSpeciesId { get; set; }

    public MediaType? MediaType { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsPublic { get; set; }

    public string? FileName { get; set; }
}