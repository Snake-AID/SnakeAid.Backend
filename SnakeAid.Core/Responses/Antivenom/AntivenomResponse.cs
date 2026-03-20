namespace SnakeAid.Core.Responses.Antivenom;

public class AntivenomResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string? Description { get; set; }
}