namespace SnakeAid.Core.Requests.LiveKit;

public class VideoGrants
{
    public string Room { get; set; } = string.Empty;
    public bool RoomJoin { get; set; } = true;
    public bool CanPublish { get; set; } = true;
    public bool CanSubscribe { get; set; } = true;
    public bool CanPublishData { get; set; } = true;
    public List<string>? CanPublishSources { get; set; }
}
