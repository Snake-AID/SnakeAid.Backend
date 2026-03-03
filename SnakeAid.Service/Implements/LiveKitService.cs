using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SnakeAid.Core.Requests.LiveKit;
using SnakeAid.Core.Responses.LiveKit;
using SnakeAid.Core.Settings;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class LiveKitService : ILiveKitService
{
    private readonly LiveKitOptions _options;
    private readonly ILiveKitApi _liveKitApi;
    private readonly ILogger<LiveKitService> _logger;

    public LiveKitService(
        IOptions<LiveKitOptions> options,
        ILiveKitApi liveKitApi,
        ILogger<LiveKitService> logger)
    {
        _options = options.Value;
        _liveKitApi = liveKitApi;
        _logger = logger;
    }

    /// <inheritdoc />
    public string GenerateAccessToken(
        string identity,
        string roomName,
        VideoGrants grants,
        string? metadata = null,
        TimeSpan? ttl = null)
    {
        var effectiveTtl = ttl ?? TimeSpan.FromMinutes(_options.TokenTtlMinutes);
        var now = DateTime.UtcNow;

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ApiSecret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Build video grants claim
        grants.Room = roomName;
        var videoGrantsJson = JsonSerializer.Serialize(grants, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var claims = new List<Claim>
        {
            new("sub", identity),
            new("video", videoGrantsJson, JsonClaimValueTypes.Json),
            new("jti", Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(metadata))
        {
            claims.Add(new Claim("metadata", metadata));
        }

        var token = new JwtSecurityToken(
            issuer: _options.ApiKey,
            notBefore: now,
            expires: now.Add(effectiveTtl),
            claims: claims,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation(
            "Generated LiveKit access token for identity={Identity}, room={Room}, ttl={TtlMinutes}m",
            identity, roomName, effectiveTtl.TotalMinutes);

        return tokenString;
    }

    /// <inheritdoc />
    public async Task<RoomInfoResponse> CreateRoomAsync(
        string roomName,
        int maxParticipants = 2,
        int emptyTimeoutSeconds = 600,
        CancellationToken cancellationToken = default)
    {
        var serverToken = GenerateServerToken();
        var request = new CreateRoomRequest
        {
            Name = roomName,
            MaxParticipants = maxParticipants,
            EmptyTimeout = emptyTimeoutSeconds
        };

        _logger.LogInformation("Creating LiveKit room: {RoomName}, maxParticipants={Max}",
            roomName, maxParticipants);

        var result = await _liveKitApi.CreateRoomAsync(request, serverToken, cancellationToken);

        _logger.LogInformation("LiveKit room created: {RoomName}, sid={Sid}",
            result.Name, result.Sid);

        return result;
    }

    /// <inheritdoc />
    public async Task DeleteRoomAsync(
        string roomName,
        CancellationToken cancellationToken = default)
    {
        var serverToken = GenerateServerToken();
        var request = new DeleteRoomRequest { Room = roomName };

        _logger.LogInformation("Deleting LiveKit room: {RoomName}", roomName);

        await _liveKitApi.DeleteRoomAsync(request, serverToken, cancellationToken);

        _logger.LogInformation("LiveKit room deleted: {RoomName}", roomName);
    }

    /// <inheritdoc />
    public async Task<List<RoomInfoResponse>> ListRoomsAsync(
        CancellationToken cancellationToken = default)
    {
        var serverToken = GenerateServerToken();

        var result = await _liveKitApi.ListRoomsAsync(new { }, serverToken, cancellationToken);

        _logger.LogInformation("Listed {Count} LiveKit rooms", result.Rooms.Count);

        return result.Rooms;
    }

    /// <inheritdoc />
    public LiveKitWebhookPayload? ValidateWebhook(
        string body, string authorizationHeader)
    {
        try
        {
            var tokenString = authorizationHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ApiSecret));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.ApiKey,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = signingKey,
                ValidateIssuerSigningKey = true
            };

            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(tokenString, validationParameters, out _);

            // Token is valid — deserialize the body
            var payload = JsonSerializer.Deserialize<LiveKitWebhookPayload>(body,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            _logger.LogInformation("LiveKit webhook validated: event={Event}, room={Room}",
                payload?.Event, payload?.Room?.Name);

            return payload;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "LiveKit webhook validation failed: invalid token");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LiveKit webhook validation error");
            return null;
        }
    }

    /// <summary>
    /// Generate a server-level JWT for Twirp API calls (roomCreate, roomList, roomDelete)
    /// </summary>
    private string GenerateServerToken()
    {
        var now = DateTime.UtcNow;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ApiSecret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Server token with roomCreate, roomList, roomAdmin grants
        var videoGrants = new
        {
            roomCreate = true,
            roomList = true,
            roomAdmin = true
        };

        var videoGrantsJson = JsonSerializer.Serialize(videoGrants, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var claims = new[]
        {
            new Claim("video", videoGrantsJson, JsonClaimValueTypes.Json),
            new Claim("jti", Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.ApiKey,
            notBefore: now,
            expires: now.AddMinutes(1),
            claims: claims,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
