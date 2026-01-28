using System.Text.Json.Serialization;

namespace SnakeAid.Core.Requests.AIVision;

/// <summary>
/// Request DTO for SnakeAI FastAPI /detect/url endpoint
/// </summary>
public class SnakeAIDetectRequest
{
    [JsonPropertyName("image_url")]
    public required string ImageUrl { get; set; }

    [JsonPropertyName("imgsz")]
    public int ImageSize { get; set; } = 640;

    [JsonPropertyName("conf")]
    public float Confidence { get; set; } = 0.25f;

    [JsonPropertyName("iou")]
    public float Iou { get; set; } = 0.5f;

    [JsonPropertyName("topk")]
    public int TopK { get; set; } = 100;

    [JsonPropertyName("save_image")]
    public bool SaveImage { get; set; } = false;
}
