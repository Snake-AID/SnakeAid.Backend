using Microsoft.AspNetCore.Http;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.LibraryMedia;

public class UpdateLibraryMediaRequest
{
    public IFormFile? File { get; set; }

    public MediaType? MediaType { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsPublic { get; set; }

    public int? SnakeSpeciesId { get; set; }
}