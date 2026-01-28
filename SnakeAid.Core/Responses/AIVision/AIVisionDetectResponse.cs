using SnakeAid.Core.Responses.AIVision;

namespace SnakeAid.Core.Responses.AIVision;

/// <summary>
/// Response from snake detection
/// </summary>
public class AIVisionDetectResponse
{
    /// <summary>
    /// AI model version used for detection
    /// </summary>
    public string? ModelVersion { get; set; }

    /// <summary>
    /// Original image width
    /// </summary>
    public int ImageWidth { get; set; }

    /// <summary>
    /// Original image height
    /// </summary>
    public int ImageHeight { get; set; }

    /// <summary>
    /// Top detected class name (YOLO class)
    /// </summary>
    public string? TopClassName { get; set; }

    /// <summary>
    /// Confidence score of top detection (0.0-1.0)
    /// </summary>
    public float? TopConfidence { get; set; }

    /// <summary>
    /// Total number of detections
    /// </summary>
    public int DetectionCount { get; set; }

    /// <summary>
    /// All detections from the model
    /// </summary>
    public List<SnakeAIDetection> Detections { get; set; } = new();

    /// <summary>
    /// Image quality warnings (blur, brightness, etc.)
    /// </summary>
    public SnakeAIWarnings? Warnings { get; set; }
}
