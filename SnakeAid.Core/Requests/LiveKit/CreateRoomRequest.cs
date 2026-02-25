namespace SnakeAid.Core.Requests.LiveKit;

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public int EmptyTimeout { get; set; } = 600;
    public int MaxParticipants { get; set; } = 2;
}
