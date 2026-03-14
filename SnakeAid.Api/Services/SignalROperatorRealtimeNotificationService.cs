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

        public async Task NotifyIncidentClaimedAsync(Guid incidentId, Guid operatorId)
        {
            try
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("IncidentClaimed", new
                {
                    IncidentId = incidentId,
                    OperatorId = operatorId,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcasted IncidentClaimed for {IncidentId} to operator group", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying incident claimed {incidentId} to operators", ex),
                    "SignalR_Operator_Notification_Error");
            }
        }

        public async Task NotifyOperatorContactingAsync(Guid incidentId, Guid operatorId)
        {
            try
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("OperatorContacting", new
                {
                    IncidentId = incidentId,
                    OperatorId = operatorId,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcasted OperatorContacting for {IncidentId} to operator group", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying operator contacting {incidentId} to operators", ex),
                    "SignalR_Operator_Notification_Error");
            }
        }

        public async Task NotifyDispatchRequestedAsync(Guid incidentId, Guid rescuerId, Guid operatorId)
        {
            try
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("DispatchRequested", new
                {
                    IncidentId = incidentId,
                    RescuerId = rescuerId,
                    OperatorId = operatorId,
                    RequestedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcasted DispatchRequested for {IncidentId} to operator group", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying dispatch requested {incidentId} to operators", ex),
                    "SignalR_Operator_Notification_Error");
            }
        }

        public async Task NotifyIncidentFalseAlarmAsync(Guid incidentId, Guid operatorId, string? reason)
        {
            try
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("IncidentFalseAlarm", new
                {
                    IncidentId = incidentId,
                    OperatorId = operatorId,
                    Reason = reason,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcasted IncidentFalseAlarm for {IncidentId} to operator group", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying incident false alarm {incidentId} to operators", ex),
                    "SignalR_Operator_Notification_Error");
            }
        }

        public async Task NotifyIncidentNoAnswerAsync(Guid incidentId, Guid operatorId, string? reason, bool continueCalling)
        {
            try
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("IncidentNoAnswer", new
                {
                    IncidentId = incidentId,
                    OperatorId = operatorId,
                    Reason = reason,
                    ContinueCalling = continueCalling,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcasted IncidentNoAnswer for {IncidentId} to operator group", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying incident no answer {incidentId} to operators", ex),
                    "SignalR_Operator_Notification_Error");
            }
        }

        public async Task NotifyRescuerDispatchedAsync(Guid incidentId, Guid rescuerId)
        {
            try
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("RescuerDispatched", new
                {
                    IncidentId = incidentId,
                    RescuerId = rescuerId,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcasted RescuerDispatched for {IncidentId} to operator group", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying rescuer dispatched {incidentId} to operators", ex),
                    "SignalR_Operator_Notification_Error");
            }
        }

        public async Task NotifyIncidentCancelledAsync(Guid incidentId, string? reason)
        {
            try
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("IncidentCancelled", new
                {
                    IncidentId = incidentId,
                    Reason = reason,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcasted IncidentCancelled for {IncidentId} to operator group", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying incident cancelled {incidentId} to operators", ex),
                    "SignalR_Operator_Notification_Error");
            }
        }

        public async Task NotifyRescuerDeclinedAsync(Guid incidentId, Guid rescuerId, string? reason)
        {
            try
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("RescuerDeclined", new
                {
                    IncidentId = incidentId,
                    RescuerId = rescuerId,
                    Reason = reason,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcasted RescuerDeclined for {IncidentId} to operator group", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying rescuer declined {incidentId} to operators", ex),
                    "SignalR_Operator_Notification_Error");
            }
        }

        public async Task NotifyRescuerAbortedAsync(Guid incidentId, Guid rescuerId, string? reason)
        {
            try
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("RescuerAborted", new
                {
                    IncidentId = incidentId,
                    RescuerId = rescuerId,
                    Reason = reason,
                    UpdatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcasted RescuerAborted for {IncidentId} to operator group", incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying rescuer aborted {incidentId} to operators", ex),
                    "SignalR_Operator_Notification_Error");
            }
        }
    }
}
