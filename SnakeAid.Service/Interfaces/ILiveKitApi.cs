using Refit;
using SnakeAid.Core.Requests.LiveKit;
using SnakeAid.Core.Responses.LiveKit;

namespace SnakeAid.Service.Interfaces;

/// <summary>
/// Refit interface for LiveKit Twirp RoomService API
/// </summary>
public interface ILiveKitApi
{
    [Post("/twirp/livekit.RoomService/CreateRoom")]
    Task<RoomInfoResponse> CreateRoomAsync(
        [Body] CreateRoomRequest request,
        [Authorize("Bearer")] string token,
        CancellationToken cancellationToken = default);

    [Post("/twirp/livekit.RoomService/DeleteRoom")]
    Task DeleteRoomAsync(
        [Body] DeleteRoomRequest request,
        [Authorize("Bearer")] string token,
        CancellationToken cancellationToken = default);

    [Post("/twirp/livekit.RoomService/ListRooms")]
    Task<ListRoomsResponse> ListRoomsAsync(
        [Body] object request,
        [Authorize("Bearer")] string token,
        CancellationToken cancellationToken = default);
}
