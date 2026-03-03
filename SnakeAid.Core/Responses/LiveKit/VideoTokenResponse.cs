namespace SnakeAid.Core.Responses.LiveKit;

public class VideoTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public string WsUrl { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
}
