namespace SnakeAid.Core.Responses.LiveKit;

public class ListRoomsResponse
{
    public List<RoomInfoResponse> Rooms { get; set; } = new();
}
