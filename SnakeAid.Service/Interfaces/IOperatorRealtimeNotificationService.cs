namespace SnakeAid.Service.Interfaces
{
    public interface IOperatorRealtimeNotificationService
    {
        Task NotifyNewIncidentCreatedAsync(Guid incidentId, Guid memberId, double latitude, double longitude);
    }
}
