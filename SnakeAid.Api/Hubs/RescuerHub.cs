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
    /// <summary>
    /// Hub for rescuers to receive rescue requests and respond.
    /// 
    /// Server-side disconnect flow:
    /// When a rescuer accepts a mission, the server sends a "ForceDisconnect" signal
    /// to the client. The client should:
    /// 1. Listen for "ForceDisconnect" event
    /// 2. Disconnect from RescuerHub gracefully
    /// 3. Join MissionHub for live mission tracking
    /// 
    /// Example client handler (TypeScript):
    /// rescuerHub.on("ForceDisconnect", (data) => {
    ///     console.log(`Disconnected: ${data.Reason}`);
    ///     rescuerHub.stop();
    ///     // Join MissionHub for mission tracking
    ///     missionHub.start();
    /// });
    /// </summary>
    public class RescuerHub : Hub
    {
        private const string OperatorGroup = "Operators";
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<RescuerHub> _logger;
        private readonly IRescuerLocationService _rescuerLocationService;
        private readonly IRescuerOnlineStatusService _onlineStatusService;
        private readonly IOperatorOnlineStatusService _operatorOnlineStatusService;
        private readonly ISnakebiteIncidentService _incidentService;

        // Static dictionary để track connected rescuers: userId -> connectionId
        public static ConcurrentDictionary<string, string> ConnectedRescuers => SignalRRescueNotificationService.ConnectedRescuers;

        public RescuerHub(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<RescuerHub> logger,
            IRescuerLocationService rescuerLocationService,
            IRescuerOnlineStatusService onlineStatusService,
            IOperatorOnlineStatusService operatorOnlineStatusService,
            ISnakebiteIncidentService incidentService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _rescuerLocationService = rescuerLocationService;
            _onlineStatusService = onlineStatusService;
            _operatorOnlineStatusService = operatorOnlineStatusService;
            _incidentService = incidentService;
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

            await Clients.Group(OperatorGroup).SendAsync("RescuerOnlineStatus", new
            {
                RescuerId = userId,
                IsOnline = true,
                IsAvailable = true,
                UpdatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Rescuer {UserId} joined with connectionId {ConnectionId}", userId, Context.ConnectionId);
            await Clients.Caller.SendAsync("Joined", new
            {
                UserId = userId,
                ConnectionId = Context.ConnectionId,
                Message = $"Rescuer {userId} joined successfully. Waiting for rescue requests..."
            });
        }

        // The method for rescuer to update location when they are idle and wait for the mission request from server.
        public async Task UpdateLocation(double latitude, double longitude)
        {
            if (!TryResolveRescuerId(out var userId))
            {
                _logger.LogWarning("UpdateLocation rejected: Invalid or missing user identifier");
                return;
            }

            try
            {
                // Update location in database (used for radius search in rescue requests)
                await _rescuerLocationService.UpdateLocationAsync(userId, latitude, longitude, null, null, null);

                await Clients.Group(OperatorGroup).SendAsync("RescuerIdleLocationUpdated", new
                {
                    RescuerId = userId,
                    Latitude = latitude,
                    Longitude = longitude,
                    UpdatedAt = DateTime.UtcNow
                });

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
                    Message = "An unexpected error occurred while updating location."
                });
            }
        }

        /// <summary>
        /// Rescuer accepts a dispatched request.
        /// </summary>
        public async Task AcceptDispatchRequest(Guid requestId)
        {
            if (!TryResolveRescuerId(out var rescuerId))
            {
                _logger.LogWarning("AcceptDispatchRequest rejected: Invalid or missing user identifier");
                await Clients.Caller.SendAsync("RequestError", new { RequestId = requestId, Error = "Invalid rescuer identifier." });
                return;
            }

            try
            {
                var response = await _incidentService.AcceptDispatchRequestAsync(requestId, rescuerId);

                await Clients.Caller.SendAsync("RequestAccepted", response);

                // Notify operators (dashboard) about acceptance
                await Clients.Group(OperatorGroup).SendAsync("RescuerAccepted", response);

                _logger.LogInformation("Rescuer {RescuerId} accepted dispatch request {RequestId}, mission {MissionId}",
                    rescuerId, requestId, response.MissionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting dispatch request {RequestId}", requestId);
                await Clients.Caller.SendAsync("RequestError", new { RequestId = requestId, Error = ex.Message });
            }
        }

        /// <summary>
        /// Rescuer declines a dispatched request.
        /// </summary>
        public async Task DeclineDispatchRequest(Guid requestId, string? reason)
        {
            if (!TryResolveRescuerId(out var rescuerId))
            {
                _logger.LogWarning("DeclineDispatchRequest rejected: Invalid or missing user identifier");
                await Clients.Caller.SendAsync("RequestError", new { RequestId = requestId, Error = "Invalid rescuer identifier." });
                return;
            }

            try
            {
                var response = await _incidentService.DeclineDispatchRequestAsync(requestId, rescuerId, reason);

                await Clients.Caller.SendAsync("RequestDeclined", response);

                // Notify operators (dashboard) about decline
                await Clients.Group(OperatorGroup).SendAsync("RescuerDeclined", new
                {
                    RequestId = response.RequestId,
                    RescuerId = rescuerId,
                    Reason = response.Message,
                    DeclinedAt = response.RejectedAt
                });

                _logger.LogInformation("Rescuer {RescuerId} declined dispatch request {RequestId}", rescuerId, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining dispatch request {RequestId}", requestId);
                await Clients.Caller.SendAsync("RequestError", new { RequestId = requestId, Error = ex.Message });
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

                await Clients.Group(OperatorGroup).SendAsync("RescuerOnlineStatus", new
                {
                    RescuerId = userId,
                    IsOnline = false,
                    IsAvailable = false,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Also remove operator tracking if this connection belonged to a connected operator
            var operatorId = Context.Items.ContainsKey("OperatorId") ? Context.Items["OperatorId"]?.ToString() : null;
            if (!string.IsNullOrEmpty(operatorId))
            {
                SignalROperatorRealtimeNotificationService.RemoveOperatorConnection(operatorId);
                _logger.LogWarning("Removed operator connection tracking for operator {OperatorId} on disconnect.", operatorId);
            }
            else
            {
                _logger.LogWarning("⚠️ OnDisconnectedAsync: no OperatorId found in Context.Items for connection {ConnectionId}", Context.ConnectionId);
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

        /// <summary>
        /// Operator joins this hub group to receive real-time rescuer idle locations and incident map events.
        /// </summary>
        public async Task JoinAsOperator(string? operatorId = null)
        {
            var role = Context.User?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            var allowed = string.Equals(role, nameof(AccountRole.Operator), StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, nameof(AccountRole.Admin), StringComparison.OrdinalIgnoreCase);

            var resolvedOperatorId = Context.UserIdentifier;

            if (!allowed && !string.IsNullOrWhiteSpace(operatorId) && Guid.TryParse(operatorId, out var operatorGuid))
            {
                var operatorAccount = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                    predicate: a => a.Id == operatorGuid,
                    asNoTracking: true);

                if (operatorAccount != null &&
                    (operatorAccount.Role == AccountRole.Operator || operatorAccount.Role == AccountRole.Admin))
                {
                    allowed = true;
                    resolvedOperatorId = operatorAccount.Id.ToString();
                }
            }

            if (!allowed)
            {
                _logger.LogWarning("JoinAsOperator rejected for connection {ConnectionId}. Role={Role}, RequestedOperatorId={OperatorId}",
                    Context.ConnectionId, role ?? "null", operatorId ?? "null");
                await Clients.Caller.SendAsync("OperatorJoinRejected", new
                {
                    Message = "Only Operator/Admin can join operator realtime group."
                });
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, OperatorGroup);
            Context.Items["OperatorId"] = resolvedOperatorId;

            // Track connected operator so we can later fall back to broadcast if they go offline
            if (!string.IsNullOrEmpty(resolvedOperatorId))
            {
                SignalROperatorRealtimeNotificationService.AddOperatorConnection(resolvedOperatorId, Context.ConnectionId);
            }

            _logger.LogInformation("Operator joined realtime group. ConnectionId={ConnectionId}", Context.ConnectionId);

            // Update operator online status (like rescuer joins)
            if (!string.IsNullOrEmpty(resolvedOperatorId))
            {
                await _operatorOnlineStatusService.SetOnDutyAsync(resolvedOperatorId);

                // Notify monitors/operators about operator availability
                await Clients.Group(OperatorGroup).SendAsync("OperatorOnlineStatus", new
                {
                    OperatorId = resolvedOperatorId,
                    IsOnDuty = true,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await Clients.Caller.SendAsync("OperatorJoined", new
            {
                Message = "Joined operator realtime group successfully.",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Operator leaves the realtime group and is marked off duty.
        /// </summary>
        public async Task LeaveAsOperator()
        {
            var operatorId = TryResolveOperatorId();
            if (!string.IsNullOrEmpty(operatorId))
            {
                await _operatorOnlineStatusService.SetOffDutyAsync(operatorId);

                // Remove operator connection tracking so fallback broadcast can occur
                SignalROperatorRealtimeNotificationService.RemoveOperatorConnection(operatorId);

                await Clients.Group(OperatorGroup).SendAsync("OperatorOnlineStatus", new
                {
                    OperatorId = operatorId,
                    IsOnDuty = false,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, OperatorGroup);
            _logger.LogInformation("Operator left realtime group. ConnectionId={ConnectionId}", Context.ConnectionId);

            await Clients.Caller.SendAsync("OperatorLeft", new
            {
                Message = "Left operator realtime group successfully.",
                Timestamp = DateTime.UtcNow
            });
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

        private bool TryResolveRescuerId(out Guid rescuerId)
        {
            rescuerId = Guid.Empty;

            var candidateIds = new[]
            {
                Context.UserIdentifier,
                Context.User?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                ConnectedRescuers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key
            };

            foreach (var candidate in candidateIds)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && Guid.TryParse(candidate, out rescuerId))
                {
                    return true;
                }
            }

            return false;
        }

        private string? TryResolveOperatorId()
        {
            var candidateIds = new[]
            {
                Context.UserIdentifier,
                Context.User?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                Context.Items.TryGetValue("OperatorId", out var operatorId) ? operatorId?.ToString() : null
            };

            return candidateIds.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
        }
    }
}
