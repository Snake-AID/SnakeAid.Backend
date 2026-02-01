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
}

public class SnakeAIWarnings
{
    public float Blur { get; set; }
    public float Brightness { get; set; }
    public float TooSmall { get; set; }
}

public class SnakeAIDetection
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public float Confidence { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}
