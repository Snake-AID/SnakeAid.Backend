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

        public SignalRMissionNotificationService(
            IHubContext<MissionHub> hubContext,
            ILogger<SignalRMissionNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

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
