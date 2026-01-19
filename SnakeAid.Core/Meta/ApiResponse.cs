using System.Text.Json.Serialization;

namespace SnakeAid.Core.Meta;

public class ApiResponse<T>
{
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")] public T? Data { get; set; }
}

public class SuccessResponse<T>
{
    [JsonPropertyName("success")] public bool Success { get; set; } = true;
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("data")] public T? Data { get; set; }
}

// Error response class - more detailed error information
public class ErrorResponse
{
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("reason")] public string? Reason { get; set; }
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}