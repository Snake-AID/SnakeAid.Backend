using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SymptomConfig;

namespace SnakeAid.Core.Responses.FirstAid;

/// <summary>
/// Response chứa first aid guideline được khuyến nghị cho snakebite incident
/// </summary>
public class FirstAidRecommendationResponse
{
    public int? GuidelineId { get; set; }
    public string GuidelineName { get; set; } = string.Empty;
    public FirstAidContent Content { get; set; } = new();
    public GuidelineSource Source { get; set; }
    
    /// <summary>
    /// Thông tin loài rắn đã được xác định (nếu có)
    /// </summary>
    public SnakeSpeciesResponse? IdentifiedSnake { get; set; }
    
    /// <summary>
    /// Context về cách xác định loài rắn (nếu có)
    /// </summary>
    public SnakeIdentificationContext? IdentificationContext { get; set; }
    
    public List<string> Warnings { get; set; } = new();
}

public enum GuidelineSource
{
    General = 0,              // Hướng dẫn chung cho rắn cắn
    SpeciesOverride = 1,      // Từ FirstAidGuidelineOverride của loài rắn cụ thể
    VenomType = 2,            // Từ VenomType.FirstAidGuideline
    NoIdentification = 3      // Chưa xác định được rắn → dùng general
}

/// <summary>
/// Metadata về cách xác định loài rắn
/// </summary>
public class SnakeIdentificationContext
{
    public SnakeIdentificationMethod Method { get; set; }
    public float? AIConfidence { get; set; }  // Nếu là AI detection
    public DateTime IdentifiedAt { get; set; }
}
