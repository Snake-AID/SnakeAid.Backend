namespace SnakeAid.Core.Responses.VenomType;

public class VenomTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? SeverityIndex { get; set; }
    public int FirstAidGuidelineId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
