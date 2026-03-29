namespace SnakeAid.Service.Interfaces
{
    public interface IOperatorRealtimeNotificationService
    {
        Task NotifyNewIncidentCreatedAsync(Guid incidentId, Guid memberId, double latitude, double longitude, string? address);

        Task NotifyIncidentClaimedAsync(Guid incidentId, Guid operatorId);

        Task NotifyIncidentCancelledAsync(Guid incidentId, string? reason);

        Task NotifyIncidentFalseAlarmAsync(Guid incidentId, Guid operatorId, string? reason);

        Task NotifyIncidentNoAnswerAsync(Guid incidentId, Guid operatorId, string? reason, bool continueCalling);

        Task NotifyDispatchRequestedAsync(Guid incidentId, Guid rescuerId, Guid operatorId);

        Task NotifyRescuerDispatchedAsync(Guid incidentId, Guid rescuerId);

        Task NotifyRescuerDeclinedAsync(Guid incidentId, Guid rescuerId, string? reason);

        Task NotifyRescuerAbortedAsync(Guid incidentId, Guid rescuerId, Guid? operatorId, string? reason);

        Task NotifyIncidentCompletedAsync(Guid incidentId, Guid rescuerId);
    }
}
