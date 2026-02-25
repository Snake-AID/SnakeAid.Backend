using System.Text.Json.Serialization;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.SnakeDetection;

public class SnakeDetectionResponse
{
    [JsonPropertyName("ai_metadata")]
    public AiMetadata Metadata { get; set; }

    [JsonPropertyName("results")]
    public List<DetectionResult> Results { get; set; } = new();

    // Optional: Keep internal ID if needed, but not in strict JSON spec unless requested. 
    // Adding it for consistency with previous system but can be hidden if needed.
    [JsonPropertyName("recognition_result_id")]
    public Guid? RecognitionResultId { get; set; }
}

public class AiMetadata
{
    [JsonPropertyName("model_version")]
    public string? ModelVersion { get; set; }

    [JsonPropertyName("image_width")]
    public int ImageWidth { get; set; }

    [JsonPropertyName("image_height")]
    public int ImageHeight { get; set; }

    [JsonPropertyName("detection_count")]
    public int DetectionCount { get; set; }

    [JsonPropertyName("warnings")]
    public SnakeAIWarnings? Warnings { get; set; }
}

public class DetectionResult
{
    [JsonPropertyName("ai_detection")]
    public AiDetection Ai { get; set; }

    [JsonPropertyName("snake")]
    public Domains.SnakeSpecies? Snake { get; set; }
}

public class AiDetection
{
    [JsonPropertyName("class_id")]
    public int ClassId { get; set; }

    [JsonPropertyName("class_name")]
    public string? ClassName { get; set; }

    [JsonPropertyName("confidence")]
    public float Confidence { get; set; }

    [JsonPropertyName("bbox")]
    public SnakeBBox BBox { get; set; }
}

public class SnakeBBox
{
    [JsonPropertyName("x1")]
    public float X1 { get; set; }

    [JsonPropertyName("y1")]
    public float Y1 { get; set; }

    [JsonPropertyName("x2")]
    public float X2 { get; set; }

    [JsonPropertyName("y2")]
    public float Y2 { get; set; }
}

public class SnakeAIWarnings
{
    [JsonPropertyName("blur")]
    public float Blur { get; set; }

    [JsonPropertyName("brightness")]
    public float Brightness { get; set; }

    [JsonPropertyName("too_small")]
    public float TooSmall { get; set; }
}
