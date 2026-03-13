using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IRescueNotificationService
    {

        bool IsRescuerConnected(string rescuerId);

        Task SendNewRequestAsync(string rescuerId, object requestData);

        Task NotifyDispatchRequestedAsync(string rescuerId, object requestData);

        Task NotifyRequestTakenAsync(string rescuerId, Guid requestId);

        Task NotifyRequestCancelledAsync(string rescuerId, Guid requestId);

        Task NotifyRequestExpiredAsync(string rescuerId, Guid requestId);

        Task ForceDisconnectRescuerAsync(string rescuerId, string reason);
    }
}
