using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.LibraryMedia;

public class CreateLibraryMediaRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;

    [Required]
    public MediaType MediaType { get; set; }

    public int? SnakeSpeciesId { get; set; }
}