using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SnakeAid.Core.Requests.SnakeSpecies;

public class CreateSnakeSpeciesFromExcelRequest
{
    [Required]
    public IFormFile ExcelFile { get; set; } = default!;

    [Required]
    public IFormFile ImageFile { get; set; } = default!;
}