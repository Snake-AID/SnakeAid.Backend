using SnakeAid.Core.Requests.LiveKit;
using SnakeAid.Core.Responses.LiveKit;

namespace SnakeAid.Service.Interfaces;

public interface ILiveKitService
{
    /// <summary>
    /// Generate a LiveKit access token (local JWT, no HTTP)
    /// </summary>
    string GenerateAccessToken(
        string identity,
        string roomName,
        VideoGrants grants,
        string? metadata = null,
        TimeSpan? ttl = null);

    /// <summary>
    /// Create a LiveKit room via Twirp API
    /// </summary>
    Task<RoomInfoResponse> CreateRoomAsync(
        string roomName,
        int maxParticipants = 2,
        int emptyTimeoutSeconds = 600,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a LiveKit room via Twirp API
    /// </summary>
    Task DeleteRoomAsync(
        string roomName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all active LiveKit rooms via Twirp API
    /// </summary>
    Task<List<RoomInfoResponse>> ListRoomsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a LiveKit webhook by verifying JWT signature (local, no HTTP)
    /// </summary>
    LiveKitWebhookPayload? ValidateWebhook(
        string body, string authorizationHeader);
}
