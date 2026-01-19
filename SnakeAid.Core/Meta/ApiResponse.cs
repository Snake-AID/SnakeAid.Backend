using System.Text.Json.Serialization;

namespace SnakeAid.Core.Meta;

public class ApiResponse<T>
{
    [JsonPropertyName("status_code")] public int StatusCode { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("is_success")] public bool IsSuccess { get; set; }
    [JsonPropertyName("data")] public T? Data { get; set; }
    public ClientErrorResponse? Error { get; set; }
}

public class ClientErrorResponse
{
    public string? ErrorCode { get; set; } // Public error codes
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; } // For form validation
}