namespace SnakeAid.Core.Settings;

public class LiveKitOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string WsUrl { get; set; } = string.Empty;
    public int TokenTtlMinutes { get; set; } = 10;
    public int RoomEmptyTimeoutSeconds { get; set; } = 600;
}
