using System;
using System.Threading.Tasks;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Core.Responses.SnakebiteIncident;

namespace SnakeAid.Service.Interfaces
{
    public interface IRescueNotificationService
    {

        bool IsRescuerConnected(string rescuerId);

        Task NotifyDispatchRequestedAsync(string rescuerId, DispatchRequestNotificationPayload requestData);

        Task NotifyRescuerAcceptedAsync(string rescuerId, AcceptRescueResponse acceptedData);

        Task NotifyRescuerDeclinedAsync(string rescuerId, RejectRescueResponse declinedData);

        Task NotifyRequestCancelledAsync(string rescuerId, Guid requestId);

        Task NotifyRequestExpiredAsync(string rescuerId, Guid requestId);

        Task ForceDisconnectRescuerAsync(string rescuerId, string reason);
    }
}
