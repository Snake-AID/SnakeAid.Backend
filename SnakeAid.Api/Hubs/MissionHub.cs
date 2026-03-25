using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Repository.Data;
using SnakeAid.Core.Domains;
using Microsoft.EntityFrameworkCore;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Hubs
{
    public class MissionHub : Hub
    {
        private const string OperatorGroup = "Operators";
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<MissionHub> _logger;
        private readonly IRescuerLocationService _rescuerLocationService;
        private readonly IHubContext<RescuerHub> _rescuerHubContext;
        private readonly IRescuerOnlineStatusService _rescuerOnlineStatusService;

        public MissionHub(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<MissionHub> logger,
            IRescuerLocationService rescuerLocationService,
            IHubContext<RescuerHub> rescuerHubContext,
            IRescuerOnlineStatusService rescuerOnlineStatusService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _rescuerLocationService = rescuerLocationService;
            _rescuerHubContext = rescuerHubContext;
            _rescuerOnlineStatusService = rescuerOnlineStatusService;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var incidentIdString = httpContext?.Request.Query["incidentId"];

            if (string.IsNullOrEmpty(incidentIdString) || !Guid.TryParse(incidentIdString, out var incidentId))
            {
                _logger.LogWarning("Connection rejected: Missing or invalid incidentId.");
                Context.Abort();
                return;
            }

            if (!TryResolveUserId(out var userId))
            {
                _logger.LogWarning("Connection rejected: Unauthenticated user or invalid UserIdentifier. Value: '{UserIdentifier}'", Context.UserIdentifier ?? "NULL");
                Context.Abort();
                return;
            }

            // Verify authorization: User must be member or assigned rescuer
            var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                predicate: i => i.Id == incidentId
            );

            if (incident == null)
            {
                _logger.LogWarning("Connection rejected: Incident {IncidentId} not found.", incidentId);
                Context.Abort();
                return;
            }

            // Allow member (incident creator) to join immediately after SOS
            // Allow assigned rescuer to join after they accept the request
            var isMember = incident.UserId == userId;
            var isAssignedRescuer = incident.AssignedRescuerId != null && incident.AssignedRescuerId == userId;

            if (!isMember && !isAssignedRescuer)
            {
                _logger.LogWarning("Connection rejected: User {UserId} not authorized for incident {IncidentId}.", userId, incidentId);
                Context.Abort();
                return;
            }

            // If rescuer is joining mission hub, mark as in mission (online + busy) during active mission.
            if (isAssignedRescuer)
            {
                await _rescuerOnlineStatusService.SetInMissionAsync(userId.ToString());

                await _rescuerHubContext.Clients.Group(OperatorGroup).SendAsync("RescuerOnlineStatus", new
                {
                    RescuerId = userId.ToString(),
                    IsOnline = true,
                    IsAvailable = false,
                    InMission = true,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            Context.Items["IncidentId"] = incidentId;
            await Groups.AddToGroupAsync(Context.ConnectionId, incidentId.ToString());

            var userRole = isMember ? "Member" : "Rescuer";
            _logger.LogInformation("{UserRole} {UserId} joined MissionHub for Incident {IncidentId}", userRole, userId, incidentId);

            // Send confirmation to caller
            await Clients.Caller.SendAsync("JoinedMissionHub", new
            {
                IncidentId = incidentId,
                UserId = userId.ToString(),
                Role = userRole,
                Message = $"Successfully joined mission tracking for incident {incidentId}",
                Timestamp = DateTime.UtcNow
            });

            await base.OnConnectedAsync();
        }

        /// Update location for both Member and Rescuer during active mission.
        /// Broadcasts location to the other party in the incident group.
        /// Only rescuer location is persisted to database (for future radius searches).
        public async Task UpdateLocation(Guid incidentId, double latitude, double longitude)
        {
            if (!Context.Items.TryGetValue("IncidentId", out var incidentObj)
                || incidentObj is not Guid connectedIncidentId
                || connectedIncidentId != incidentId)
            {
                _logger.LogWarning("UpdateLocation rejected: Incident context mismatch. Connected={ConnectedIncidentId}, Payload={PayloadIncidentId}",
                    incidentObj, incidentId);
                await Clients.Caller.SendAsync("LocationError", new
                {
                    Error = "InvalidIncidentContext"
                });
                return;
            }

            if (!TryResolveUserId(out var userId))
            {
                _logger.LogWarning("UpdateLocation rejected: Invalid user identifier");
                return;
            }

            // Validate coordinates
            if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            {
                _logger.LogWarning("UpdateLocation rejected: Invalid coordinates ({Lat}, {Lng})", latitude, longitude);
                return;
            }

            // Get incident to identify sender role
            var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                predicate: i => i.Id == incidentId
            );

            if (incident == null)
            {
                _logger.LogWarning("UpdateLocation rejected: Incident {IncidentId} not found", incidentId);
                return;
            }

            // Identify sender: Member or Rescuer
            var isMember = incident.UserId == userId;
            var isAssignedRescuer = incident.AssignedRescuerId != null && incident.AssignedRescuerId == userId;

            if (!isMember && !isAssignedRescuer)
            {
                _logger.LogWarning("UpdateLocation rejected: User {UserId} not authorized for incident {IncidentId}", userId, incidentId);
                return;
            }

            var senderRole = isMember ? "Member" : "Rescuer";
            var eventName = isMember ? "MemberLocationUpdated" : "RescuerLocationUpdated";

            // Rescuer location updates is persisted to database
            // Member location is only broadcasted via mission hub and not stored
            if (isAssignedRescuer)
            {
                try
                {
                    await _rescuerLocationService.UpdateLocationAsync(userId, latitude, longitude, null, null, null);
                    _logger.LogDebug("Rescuer {UserId} location saved to database", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save rescuer location to database");
                }
            }

            // Broadcast to Group (Member and Rescuer can see each other)
            await Clients.Group(incidentId.ToString()).SendAsync(eventName, new
            {
                UserId = userId.ToString(),
                Role = senderRole,
                Latitude = latitude,
                Longitude = longitude,
                UpdatedAt = DateTime.UtcNow
            });

            // Broadcast member incident location ping to operator dashboard map.
            if (isMember)
            {
                await _rescuerHubContext.Clients.Group(OperatorGroup).SendAsync("IncidentLocationUpdated", new
                {
                    IncidentId = incidentId,
                    MemberId = userId,
                    Latitude = latitude,
                    Longitude = longitude,
                    IsNewIncident = false,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Broadcast rescuer mission location to operator dashboard map (mission-mode tracking)
            if (isAssignedRescuer)
            {
                await _rescuerHubContext.Clients.Group(OperatorGroup).SendAsync("RescuerMissionLocationUpdated", new
                {
                    IncidentId = incidentId,
                    RescuerId = userId,
                    Latitude = latitude,
                    Longitude = longitude,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation("{Role} {UserId} location updated in Incident {IncidentId}: ({Lat}, {Lng})",
                senderRole, userId, incidentId, latitude, longitude);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            var incidentIdString = httpContext?.Request.Query["incidentId"];

            if (!string.IsNullOrEmpty(incidentIdString) && Guid.TryParse(incidentIdString, out var incidentId))
            {
                if (TryResolveUserId(out var userId))
                {
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId
                    );

                    if (incident != null)
                    {
                        var isAssignedRescuer = incident.AssignedRescuerId != null && incident.AssignedRescuerId == userId;

                        // If rescuer is disconnecting from mission hub, restore their online + available status
                        if (isAssignedRescuer)
                        {
                            await _rescuerOnlineStatusService.SetOnlineAsync(userId.ToString());

                            await _rescuerHubContext.Clients.Group(OperatorGroup).SendAsync("RescuerOnlineStatus", new
                            {
                                RescuerId = userId.ToString(),
                                IsOnline = true,
                                IsAvailable = true,
                                InMission = false,
                                UpdatedAt = DateTime.UtcNow
                            });

                            // Notify that mission is completed
                            await _rescuerHubContext.Clients.Group(OperatorGroup).SendAsync("MissionCompleted", new
                            {
                                IncidentId = incidentId,
                                RescuerId = userId.ToString(),
                                CompletedAt = DateTime.UtcNow
                            });

                            _logger.LogInformation("Rescuer {UserId} disconnected from MissionHub for incident {IncidentId}, restored to available status", userId, incidentId);
                        }
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private bool TryResolveUserId(out Guid userId)
        {
            userId = Guid.Empty;

            var candidateIds = new[]
            {
                Context.UserIdentifier,
                Context.User?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            };

            foreach (var candidate in candidateIds)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && Guid.TryParse(candidate, out userId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
