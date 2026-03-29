using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IMissionNotificationService
    {
        Task NotifyRescuerAcceptedAsync(Guid incidentId, object rescuerInfo);
        Task NotifyOperatorContactingAsync(Guid incidentId, object data);
        Task NotifyRescuerDispatchedAsync(Guid incidentId, object data);
        Task NotifyRescuerDeclinedAsync(Guid incidentId, object data);
        Task NotifyMissionStartedAsync(Guid incidentId, object missionInfo);
        Task NotifyRescuerArrivedAsync(Guid incidentId);
        Task NotifyMissionCompletedAsync(Guid incidentId, object result);
        Task NotifyMissionCancelledAsync(Guid incidentId, string reason);
        Task NotifyMissionAbortedAsync(Guid incidentId, string reason);
        Task NotifyRescuerLocationUpdateAsync(Guid incidentId, double latitude, double longitude);
        Task NotifyMemberSessionExpiredAsync(Guid incidentId);
    }
}
