using System.Text.Json.Serialization;

namespace SnakeAid.Core.Responses.AIVision;

/// <summary>
/// Response from SnakeAI FastAPI /detect/url endpoint
/// </summary>
public class SnakeAIDetectResponse
{
    [JsonPropertyName("model_version")]
    public string ModelVersion { get; set; } = string.Empty;

    [JsonPropertyName("image_width")]
    public int ImageWidth { get; set; }

    [JsonPropertyName("image_height")]
    public int ImageHeight { get; set; }

    [JsonPropertyName("warnings")]
    public SnakeAIWarnings? Warnings { get; set; }

    [JsonPropertyName("detections")]
    public List<SnakeAIDetection> Detections { get; set; } = new();
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

public class SnakeAIDetection
{
    [JsonPropertyName("class_id")]
    public int ClassId { get; set; }

    [JsonPropertyName("class_name")]
    public string ClassName { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public float Confidence { get; set; }

    [JsonPropertyName("bbox")]
    public BoundingBox Bbox { get; set; } = new();
}

public class BoundingBox
{
    [JsonPropertyName("x1")]
    public int X1 { get; set; }

    [JsonPropertyName("y1")]
    public int Y1 { get; set; }

    [JsonPropertyName("x2")]
    public int X2 { get; set; }

    [JsonPropertyName("y2")]
    public int Y2 { get; set; }
}

/// <summary>
/// Response from SnakeAI FastAPI /health endpoint
/// </summary>
public class SnakeAIHealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("model_loaded")]
    public bool ModelLoaded { get; set; }

    [JsonPropertyName("model_version")]
    public string ModelVersion { get; set; } = string.Empty;

    [JsonPropertyName("uptime_s")]
    public int UptimeSeconds { get; set; }
}
