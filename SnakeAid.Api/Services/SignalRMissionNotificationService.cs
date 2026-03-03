using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SnakeAid.Api.Hubs;
using SnakeAid.Service.Interfaces;
using SnakeAid.Core.Exceptions;

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
            => await SafeExecuteAsync(() => _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionStarted", missionInfo),
                "MissionStarted", incidentId);

        public async Task NotifyRescuerArrivedAsync(Guid incidentId)
            => await SafeExecuteAsync(() => _hubContext.Clients.Group(incidentId.ToString()).SendAsync("RescuerArrived"),
                "RescuerArrived", incidentId);

        public async Task NotifyMissionCompletedAsync(Guid incidentId, object result)
            => await SafeExecuteAsync(() => _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionCompleted", result),
                "MissionCompleted", incidentId);

        public async Task NotifyMissionCancelledAsync(Guid incidentId, string reason)
            => await SafeExecuteAsync(() => _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionCancelled", new { Reason = reason }),
                "MissionCancelled", incidentId);

        public async Task NotifyRescuerLocationUpdateAsync(Guid incidentId, double latitude, double longitude)
            => await SafeExecuteAsync(() => _hubContext.Clients.Group(incidentId.ToString()).SendAsync("LocationUpdated", new
            {
                Latitude = latitude,
                Longitude = longitude,
                UpdatedAt = DateTime.UtcNow
            }), "RescuerLocationUpdate", incidentId);

        public async Task NotifyMemberSessionExpiredAsync(Guid incidentId)
            => await SafeExecuteAsync(() => _hubContext.Clients.Group(incidentId.ToString()).SendAsync("SessionExpired"),
                "SessionExpired", incidentId);

        private async Task SafeExecuteAsync(Func<Task> action, string actionName, Guid incidentId)
        {
            try
            {
                await action();
                _logger.LogInformation("Notified {Action} for incident {IncidentId}", actionName, incidentId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying {actionName} for incident {incidentId}", ex),
                    "SignalR_Notification_Error");
            }
        }
    }
}
