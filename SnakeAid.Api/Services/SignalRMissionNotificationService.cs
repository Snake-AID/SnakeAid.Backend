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

        public SignalRMissionNotificationService(IHubContext<MissionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyRescuerAcceptedAsync(Guid incidentId, object rescuerInfo)
        {
            await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("RescuerAccepted", rescuerInfo);
        }

        public async Task NotifyMissionStartedAsync(Guid incidentId, object missionInfo)
        {
            await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionStarted", missionInfo);
        }

        public async Task NotifyRescuerArrivedAsync(Guid incidentId)
        {
            await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("RescuerArrived");
        }

        public async Task NotifyMissionCompletedAsync(Guid incidentId, object result)
        {
            await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionCompleted", result);
        }

        public async Task NotifyMissionCancelledAsync(Guid incidentId, string reason)
        {
            await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionCancelled", new { Reason = reason });
        }

        public async Task NotifyRescuerLocationUpdateAsync(Guid incidentId, double latitude, double longitude)
        {
            await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("LocationUpdated", new
            {
                Latitude = latitude,
                Longitude = longitude,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public async Task NotifyMemberSessionExpiredAsync(Guid incidentId)
        {
            await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("SessionExpired");
        }
    }
}
