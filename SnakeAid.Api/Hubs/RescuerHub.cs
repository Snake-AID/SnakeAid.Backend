using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Api.Services;
using SnakeAid.Service.Interfaces;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Repository.Data;

namespace SnakeAid.Api.Hubs
{
    public class RescuerHub : Hub
    {
        private readonly IRescueRequestSessionService _sessionService;
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<RescuerHub> _logger;
        private readonly IRescuerLocationService _rescuerLocationService;
        private readonly IRescuerOnlineStatusService _onlineStatusService;

        // Static dictionary để track connected rescuers: userId -> connectionId
        public static ConcurrentDictionary<string, string> ConnectedRescuers => SignalRRescueNotificationService.ConnectedRescuers;

        public RescuerHub(
            IRescueRequestSessionService sessionService,
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<RescuerHub> logger,
            IRescuerLocationService rescuerLocationService,
            IRescuerOnlineStatusService onlineStatusService)
        {
            _sessionService = sessionService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _rescuerLocationService = rescuerLocationService;
            _onlineStatusService = onlineStatusService;
        }

        /// Rescuer joins the hub to receice rescue request from server
        public async Task JoinAsRescuer(string userId)
        {
            _logger.LogInformation("JoinAsRescuer called for userId: {UserId}, ConnectionId: {ConnectionId}, Current dictionary size: {DictSize}",
                userId, Context.ConnectionId, SignalRRescueNotificationService.ConnectedRescuers.Count);

            var rescuerInfo = await GetRescuerBriefInfoAsync(userId);

            // Notify monitors about rescuer joining
            await Clients.Group("Monitors").SendAsync("AdminLog", new
            {
                Type = "RescuerJoined",
                UserId = userId,
                Rescuer = rescuerInfo,
                Message = $"Rescuer {userId} joined.",
                Timestamp = DateTime.UtcNow
            });

            // Add connection to notification service
            SignalRRescueNotificationService.AddConnection(userId, Context.ConnectionId);

            _logger.LogInformation("After AddConnection: Dictionary size: {DictSize}, Contains {UserId}: {Contains}",
                SignalRRescueNotificationService.ConnectedRescuers.Count,
                userId,
                SignalRRescueNotificationService.ConnectedRescuers.ContainsKey(userId));

            // Update RescuerProfile IsOnline status in database
            await _onlineStatusService.SetOnlineAsync(userId);

            _logger.LogInformation("Rescuer {UserId} joined with connectionId {ConnectionId}", userId, Context.ConnectionId);
            await Clients.Caller.SendAsync("Joined", new
            {
                UserId = userId,
                ConnectionId = Context.ConnectionId,
                Message = $"Rescuer {userId} joined successfully. Waiting for rescue requests..."
            });
        }

        /// Rescuer accept request - Ai accept nhanh nhất sẽ nhận mission
        public async Task AcceptRequest(Guid requestId, Guid rescuerId)
        {
            try
            {
                var result = await _sessionService.AcceptRequestAsync(requestId, rescuerId);

                var response = new
                {
                    RequestId = result.RequestId,
                    IncidentId = result.IncidentId,
                    MissionId = result.MissionId,
                    AcceptedAt = result.AcceptedAt,
                    Message = result.Message
                };

                // Send mission info back to client for navigation to mission detail screen
                await Clients.Caller.SendAsync("RequestAccepted", response);

                _logger.LogInformation("Rescuer {RescuerId} accepted request {RequestId}, mission {MissionId} created",
                    rescuerId, requestId, result.MissionId);
                _logger.LogInformation("Response sent to client: {Response}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting request {RequestId}: {Message}", requestId, ex.Message);
                await Clients.Caller.SendAsync("RequestError", new
                {
                    RequestId = requestId,
                    Error = ex.Message
                });
            }
        }

        // The method for rescuer to update location when they are idle and wait for the mission request from server.
        public async Task UpdateLocation(double latitude, double longitude)
        {
            var userIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("UpdateLocation rejected: Invalid or missing user identifier");
                return;
            }

            try
            {
                // Update location in database (used for radius search in rescue requests)
                await _rescuerLocationService.UpdateLocationAsync(userId, latitude, longitude, null, null, null);

                _logger.LogDebug("Idle location updated for rescuer {UserId}: ({Lat}, {Lng})",
                    userId, latitude, longitude);

                // Confirm back to rescuer
                await Clients.Caller.SendAsync("LocationUpdated", new
                {
                    UserId = userId,
                    Latitude = latitude,
                    Longitude = longitude,
                    UpdatedAt = DateTime.UtcNow,
                    Message = "Location updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location for rescuer {UserId}", userId);
                await Clients.Caller.SendAsync("LocationError", new
                {
                    Error = "Failed to update location",
                    Message = ex.Message
                });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            _logger.LogWarning("⚠️ OnDisconnectedAsync START. ConnectionId: {ConnectionId}, Dictionary size: {DictSize}, Exception: {Exception}",
                connectionId, SignalRRescueNotificationService.ConnectedRescuers.Count, exception?.Message ?? "None");

            // Log all current connections
            _logger.LogWarning("Current connections: {Connections}",
                string.Join(", ", SignalRRescueNotificationService.ConnectedRescuers.Select(kvp => $"{kvp.Key}→{kvp.Value}")));

            var userId = ConnectedRescuers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (userId != null)
            {
                _logger.LogWarning("Found userId {UserId} for disconnected ConnectionId {ConnectionId}", userId, connectionId);

                SignalRRescueNotificationService.RemoveConnection(userId);

                _logger.LogWarning("After removal, Dictionary size: {DictSize}", SignalRRescueNotificationService.ConnectedRescuers.Count);

                // Notify Monitors
                await Clients.Group("Monitors").SendAsync("AdminLog", new
                {
                    Type = "RescuerDisconnected",
                    UserId = userId,
                    Message = $"Rescuer {userId} disconnected.",
                    Timestamp = DateTime.UtcNow
                });

                // Update RescuerProfile IsOnline status in database
                await _onlineStatusService.SetOfflineAsync(userId);
            }
            else
            {
                _logger.LogWarning("⚠️ OnDisconnectedAsync called for UNKNOWN connection {ConnectionId} (never called JoinAsRescuer?)", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);

            _logger.LogWarning("⚠️ OnDisconnectedAsync END. Final dictionary size: {DictSize}", SignalRRescueNotificationService.ConnectedRescuers.Count);
        }

        public async Task GetConnectedRescuers()
        {
            var rescuerIds = ConnectedRescuers.Keys.ToList();
            var rescuers = new List<object>();

            foreach (var id in rescuerIds)
            {
                rescuers.Add(await GetRescuerBriefInfoAsync(id));
            }

            await Clients.Caller.SendAsync("ConnectedRescuers", new
            {
                Count = rescuers.Count,
                Rescuers = rescuers
            });
        }

        /// <summary>
        /// Admin Dashboard caller joins Monitors group
        /// </summary>
        public async Task JoinAsMonitor()
        {
            // Optional: User.IsInRole("Admin") can be checked here if authorization policy hasn't caught it
            await Groups.AddToGroupAsync(Context.ConnectionId, "Monitors");

            _logger.LogInformation("Admin joined as Monitor with ConnectionId: {ConnectionId}", Context.ConnectionId);

            await Clients.Caller.SendAsync("AdminLog", new
            {
                Type = "MonitorJoined",
                Message = "Connected to Monitor group successfully.",
                Timestamp = DateTime.UtcNow
            });

            await GetConnectedRescuers();
        }

        private async Task<object> GetRescuerBriefInfoAsync(string userId)
        {
            if (Guid.TryParse(userId, out var rescuerGuid))
            {
                var user = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                    predicate: a => a.Id == rescuerGuid,
                    asNoTracking: true
                );

                if (user != null)
                {
                    var profile = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                        predicate: rp => rp.AccountId == rescuerGuid,
                        asNoTracking: true
                    );

                    _logger.LogInformation("GetRescuerBriefInfoAsync: Found user {UserId}, FullName='{FullName}', HasProfile={HasProfile}",
                        userId, user.FullName, profile != null);

                    return new
                    {
                        Id = userId,
                        FullName = string.IsNullOrWhiteSpace(user.FullName) ? "Unknown Rescuer" : user.FullName,
                        AvatarUrl = user.AvatarUrl,
                        Type = profile?.Type.ToString() ?? "Unknown",
                        Rating = profile?.Rating ?? 0,
                        TotalMissions = profile?.TotalMissions ?? 0,
                        IsOnline = profile?.IsOnline ?? false
                    };
                }
                else
                {
                    _logger.LogWarning("GetRescuerBriefInfoAsync: Account strictly NOT FOUND in DbContext for userId {UserId}. This means the Guid does NOT exist in AspNetUsers table.", userId);
                }
            }
            else
            {
                _logger.LogWarning("GetRescuerBriefInfoAsync: Invalid Guid format for userId {UserId}", userId);
            }

            return new { Id = userId, FullName = "Unknown Rescuer", Type = "Unknown", Rating = 0, TotalMissions = 0, IsOnline = false };
        }
    }
}