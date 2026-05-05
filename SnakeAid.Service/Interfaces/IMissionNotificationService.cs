using System;
using System.Threading.Tasks;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Core.Responses.SnakebiteIncident;

namespace SnakeAid.Service.Interfaces
{
    public interface IMissionNotificationService
    {
        Task NotifyRescuerAcceptedAsync(Guid incidentId, Guid memberUserId, AcceptRescueResponse rescuerInfo);
        Task NotifyMissionStartedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, MissionStartedNotificationPayload missionInfo);
        Task NotifyRescuerArrivedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, string? rescuerName = null);
        Task NotifyMissionCompletedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, MissionCompletedNotificationPayload result);
        Task NotifyMissionCancelledAsync(Guid incidentId, Guid rescuerUserId, string reason);
        Task NotifyMissionAbortedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, string? rescuerName, string reason);
        Task NotifyIncidentFalseAlarmAsync(Guid incidentId, Guid memberUserId, string? reason);
        Task NotifyHospitalHandoverAcceptedAsync(Guid incidentId, Guid memberUserId, string hospitalName, string? hospitalPhone, string? operatorNote);
    }
}
