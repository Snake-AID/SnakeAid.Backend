using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SnakeAid.Api.Hubs;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Services
{
    public class SignalRMissionNotificationService : IMissionNotificationService
    {
        private readonly IHubContext<MissionHub> _hubContext;
        private readonly ILogger<SignalRMissionNotificationService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="SignalRMissionNotificationService"/> with the provided SignalR hub context and logger.
        /// </summary>
        public SignalRMissionNotificationService(
            IHubContext<MissionHub> hubContext,
            ILogger<SignalRMissionNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Notify all clients in the SignalR group for the given incident that a mission has started.
        /// </summary>
        /// <param name="incidentId">The incident identifier used to select the SignalR group to notify.</param>
        /// <param name="missionInfo">An object containing mission details to include in the notification payload.</param>
        public async Task NotifyMissionStartedAsync(Guid incidentId, object missionInfo)
        {
            try
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionStarted", missionInfo);
                _logger.LogInformation("Notified MissionStarted for incident {IncidentId}", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying MissionStarted for incident {IncidentId}: {Message}", incidentId, ex.Message);
            }
        }

        /// <summary>
        /// Sends a "RescuerArrived" notification to all clients in the SignalR group for the specified incident.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident whose group should receive the notification.</param>
        public async Task NotifyRescuerArrivedAsync(Guid incidentId)
        {
            try
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("RescuerArrived");
                _logger.LogInformation("Notified RescuerArrived for incident {IncidentId}", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying RescuerArrived for incident {IncidentId}: {Message}", incidentId, ex.Message);
            }
        }

        /// <summary>
        /// Sends a "MissionCompleted" notification with the provided result to all SignalR clients in the group for the given incident.
        /// </summary>
        /// <param name="incidentId">Identifier of the incident whose client group will receive the notification.</param>
        /// <param name="result">Payload describing the mission outcome sent to clients.</param>
        public async Task NotifyMissionCompletedAsync(Guid incidentId, object result)
        {
            try
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionCompleted", result);
                _logger.LogInformation("Notified MissionCompleted for incident {IncidentId}", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying MissionCompleted for incident {IncidentId}: {Message}", incidentId, ex.Message);
            }
        }

        /// <summary>
        /// Sends a "MissionCancelled" notification to all SignalR clients in the group for the specified incident.
        /// </summary>
        /// <param name="incidentId">The incident identifier whose client group will receive the notification.</param>
        /// <param name="reason">A human-readable reason for the mission cancellation included in the notification payload.</param>
        public async Task NotifyMissionCancelledAsync(Guid incidentId, string reason)
        {
            try
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionCancelled", new { Reason = reason });
                _logger.LogInformation("Notified MissionCancelled for incident {IncidentId}", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying MissionCancelled for incident {IncidentId}: {Message}", incidentId, ex.Message);
            }
        }

        /// <summary>
        /// Sends a "LocationUpdated" notification to the SignalR group for the specified incident containing the rescuer's coordinates and the update timestamp.
        /// </summary>
        /// <param name="incidentId">Identifier of the incident whose SignalR group will receive the notification.</param>
        /// <param name="latitude">Rescuer latitude in decimal degrees.</param>
        /// <param name="longitude">Rescuer longitude in decimal degrees.</param>
        public async Task NotifyRescuerLocationUpdateAsync(Guid incidentId, double latitude, double longitude)
        {
            try
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("LocationUpdated", new
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying RescuerLocationUpdate for incident {IncidentId}: {Message}", incidentId, ex.Message);
            }
        }

        /// <summary>
        /// Notify all clients in the group for the specified incident that the member session has expired.
        /// </summary>
        /// <param name="incidentId">The incident identifier whose SignalR group will receive the SessionExpired message.</param>
        /// <remarks>
        /// Exceptions thrown while sending the notification are caught and logged; they are not propagated to the caller.
        /// </remarks>
        public async Task NotifyMemberSessionExpiredAsync(Guid incidentId)
        {
            try
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("SessionExpired");
                _logger.LogInformation("Notified SessionExpired for incident {IncidentId}", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying SessionExpired for incident {IncidentId}: {Message}", incidentId, ex.Message);
            }
        }
    }
}
