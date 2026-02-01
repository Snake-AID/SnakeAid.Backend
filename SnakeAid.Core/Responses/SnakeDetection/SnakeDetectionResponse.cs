namespace SnakeAid.Core.Responses.SnakeDetection;

public class SnakeDetectionResponse
{
    public string? ModelVersion { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public string? TopClassName { get; set; }
    public float? TopConfidence { get; set; }
    public int DetectionCount { get; set; }
    public List<SnakeAIDetection> Detections { get; set; } = new();
    public SnakeAIWarnings? Warnings { get; set; }
    
    /// <summary>
    /// ID of the saved recognition result in database
    /// </summary>
    public Guid? RecognitionResultId { get; set; }
}

public class SnakeAIWarnings
{
    public float Blur { get; set; }
    public float Brightness { get; set; }
    public float TooSmall { get; set; }
}

public class SnakeAIDetection
{
    // YOLO Detection fields
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public float Confidence { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    
    // Species info from mapping (Phase 2)
    /// <summary>
    /// Mapped SnakeSpecies ID (null if not mapped)
    /// </summary>
    public int? SpeciesId { get; set; }
    
    /// <summary>
    /// Common name of the species (Vietnamese)
    /// </summary>
    public string? SpeciesName { get; set; }
    
    /// <summary>
    /// Scientific name of the species
    /// </summary>
    public string? ScientificName { get; set; }
    
    /// <summary>
    /// Whether the species is venomous
    /// </summary>
    public bool? IsVenomous { get; set; }
    
    /// <summary>
    /// Risk level from 0-10
    /// </summary>
    public float? RiskLevel { get; set; }
}
