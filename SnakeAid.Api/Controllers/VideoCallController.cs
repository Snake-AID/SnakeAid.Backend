using System.Text;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using SnakeAid.Core.Requests.LiveKit;
using SnakeAid.Core.Responses.LiveKit;
using SnakeAid.Core.Settings;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/videocall")]
public class VideoCallController : BaseController<VideoCallController>
{
    private readonly ILiveKitService _liveKitService;
    private readonly LiveKitOptions _liveKitOptions;
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

    public VideoCallController(
        ILogger<VideoCallController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ILiveKitService liveKitService,
        IOptions<LiveKitOptions> liveKitOptions,
        IUnitOfWork<SnakeAidDbContext> unitOfWork)
        : base(logger, httpContextAccessor, mapper)
    {
        _liveKitService = liveKitService;
        _liveKitOptions = liveKitOptions.Value;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Generate LiveKit video token for a consultation
    /// </summary>
    [HttpPost("/api/consultations/{consultationId:guid}/video-token")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Generate LiveKit video token for consultation",
        Description = "Generates a LiveKit access token for the authenticated user to join a consultation video call.",
        Tags = new[] { "VideoCall" })]
    [ProducesResponseType(typeof(ApiResponse<VideoTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateVideoToken(
        Guid consultationId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        var consultation = await _unitOfWork.GetRepository<SnakeAid.Core.Domains.Consultation>()
            .FirstOrDefaultAsync(predicate: c => c.Id == consultationId, cancellationToken: cancellationToken);

        if (consultation == null)
        {
            throw new NotFoundException("Consultation not found.");
        }

        var isParticipant = consultation.CallerId == userId || consultation.CalleeId == userId;
        var isAdmin = userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        if (!isParticipant && !isAdmin)
        {
            throw new ForbiddenException("You are not allowed to access this consultation room.");
        }

        if (string.IsNullOrWhiteSpace(consultation.RoomId))
        {
            throw new ValidationException("Consultation room is not initialized.");
        }

        var roomName = consultation.RoomId;
        var grants = BuildGrants(roomName, userRole);

        var token = _liveKitService.GenerateAccessToken(
            identity: userId.ToString(),
            roomName: roomName,
            grants: grants,
            metadata: $"{{\"role\":\"{userRole}\",\"consultationId\":\"{consultationId}\"}}");

        var response = new VideoTokenResponse
        {
            Token = token,
            WsUrl = _liveKitOptions.WsUrl,
            RoomName = roomName
        };

        return Ok(ApiResponseBuilder.BuildSuccessResponse(response, "Video token generated successfully"));
    }

    /// <summary>
    /// [DEV] Generate LiveKit video token for testing with custom room name
    /// </summary>
    [HttpPost("livekit-token/demo/{roomname}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "[DEV] Generate LiveKit video token for testing",
        Description = "Development-only endpoint. Generates a LiveKit token using a custom room name, bypassing consultation validation.",
        Tags = new[] { "VideoCall" })]
    [ProducesResponseType(typeof(ApiResponse<VideoTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GenerateDemoVideoToken(
        string roomname,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var grants = BuildGrants(roomname, userRole);

        var token = _liveKitService.GenerateAccessToken(
            identity: userId.ToString(),
            roomName: roomname,
            grants: grants,
            metadata: $"{{\"role\":\"{userRole}\",\"demo\":true}}");

        var response = new VideoTokenResponse
        {
            Token = token,
            WsUrl = _liveKitOptions.WsUrl,
            RoomName = roomname
        };

        return Task.FromResult<IActionResult>(Ok(ApiResponseBuilder.BuildSuccessResponse(response, "Demo video token generated successfully")));
    }

    /// <summary>
    /// LiveKit webhook endpoint
    /// </summary>
    [HttpPost("livekit-webhook")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "LiveKit webhook endpoint",
        Description = "Receives LiveKit event notifications (room_started, room_finished, participant_joined, etc.).",
        Tags = new[] { "VideoCall" })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader))
        {
            _logger.LogWarning("LiveKit webhook received without Authorization header");
            throw new BadRequestException("Missing Authorization header");
        }

        var payload = _liveKitService.ValidateWebhook(rawBody, authHeader);
        if (payload is null)
        {
            _logger.LogWarning("LiveKit webhook validation failed");
            throw new BadRequestException("Invalid webhook signature");
        }

        _logger.LogInformation("LiveKit webhook: event={Event}, room={RoomName}",
            payload.Event, payload.Room?.Name);

        switch (payload.Event)
        {
            case "room_started":
                _logger.LogInformation("Room started: {RoomName}", payload.Room?.Name);
                break;
            case "room_finished":
                _logger.LogInformation("Room finished: {RoomName}", payload.Room?.Name);
                break;
            case "participant_joined":
                _logger.LogInformation("Participant joined: {Identity} in room {RoomName}",
                    payload.Participant?.Identity, payload.Room?.Name);
                break;
            case "participant_left":
                _logger.LogInformation("Participant left: {Identity} from room {RoomName}",
                    payload.Participant?.Identity, payload.Room?.Name);
                break;
            default:
                _logger.LogInformation("Unhandled LiveKit event: {Event}", payload.Event);
                break;
        }

        return Ok(ApiResponseBuilder.BuildSuccessResponse("Webhook processed"));
    }

    /// <summary>
    /// Build VideoGrants based on user role
    /// </summary>
    private static VideoGrants BuildGrants(string roomName, string userRole)
    {
        return new VideoGrants
        {
            Room = roomName,
            RoomJoin = true,
            CanPublish = true,
            CanSubscribe = true,
            CanPublishData = true,
            CanPublishSources = userRole.Equals("Expert", StringComparison.OrdinalIgnoreCase)
                ? new List<string> { "camera", "microphone", "screen_share", "screen_share_audio" }
                : new List<string> { "camera", "microphone" }
        };
    }
}
