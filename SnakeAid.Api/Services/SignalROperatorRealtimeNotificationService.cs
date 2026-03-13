using Microsoft.AspNetCore.SignalR;
using SnakeAid.Api.Hubs;
using SnakeAid.Core.Exceptions;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Services
{
    public class SignalROperatorRealtimeNotificationService : IOperatorRealtimeNotificationService
    {
        private const string OperatorGroup = "Operators";
        private readonly IHubContext<RescuerHub> _hubContext;
        private readonly ILogger<SignalROperatorRealtimeNotificationService> _logger;

        public SignalROperatorRealtimeNotificationService(
            IHubContext<RescuerHub> hubContext,
            ILogger<SignalROperatorRealtimeNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyNewIncidentCreatedAsync(Guid incidentId, Guid memberId, double latitude, double longitude)
        {
            try
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("IncidentLocationUpdated", new
                {
                    IncidentId = incidentId,
                    MemberId = memberId,
                    Latitude = latitude,
                    Longitude = longitude,
                    IsNewIncident = true,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcasted new incident location {IncidentId} to operator group", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying new incident {incidentId} to operators", ex),
                    "SignalR_Operator_Notification_Error");
            }
        }
    }
}
