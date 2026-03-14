namespace SnakeAid.Service.Interfaces
{
    public interface IOperatorRealtimeNotificationService
    {
        Task NotifyNewIncidentCreatedAsync(Guid incidentId, Guid memberId, double latitude, double longitude);

        Task NotifyIncidentClaimedAsync(Guid incidentId, Guid operatorId);

        Task NotifyDispatchRequestedAsync(Guid incidentId, Guid rescuerId, Guid operatorId);

        Task NotifyRescuerDispatchedAsync(Guid incidentId, Guid rescuerId);

        Task NotifyRescuerDeclinedAsync(Guid incidentId, Guid rescuerId, string? reason);
    }
}
