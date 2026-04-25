using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.VenomType;

public class CreateVenomTypeRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    [Required]
    public string ScientificName { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Range(1, 10)]
    public int? SeverityIndex { get; set; }

    [Required]
    public int FirstAidGuidelineId { get; set; }
}
