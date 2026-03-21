using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.VenomType;

public class UpdateVenomTypeRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? ScientificName { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    [Range(1, 10)]
    public int? SeverityIndex { get; set; }

    public int? FirstAidGuidelineId { get; set; }
}
