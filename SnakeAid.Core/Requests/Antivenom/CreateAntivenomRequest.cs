using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Antivenom;

public class CreateAntivenomRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Manufacturer { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}