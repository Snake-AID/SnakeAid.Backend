namespace SnakeAid.Core.Responses.LiveKit;

public class RoomInfoResponse
{
    public string Name { get; set; } = string.Empty;
    public string Sid { get; set; } = string.Empty;
    public int NumParticipants { get; set; }
    public long CreationTime { get; set; }
}
