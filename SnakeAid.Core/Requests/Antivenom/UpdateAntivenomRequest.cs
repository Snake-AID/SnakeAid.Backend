using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Antivenom;

public class UpdateAntivenomRequest
{
    public string? Name { get; set; }

    [StringLength(200)]
    public string? Manufacturer { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}