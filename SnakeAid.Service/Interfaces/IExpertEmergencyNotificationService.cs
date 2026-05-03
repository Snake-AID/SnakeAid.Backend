using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IExpertEmergencyNotificationService
    {
        bool IsExpertConnected(string expertId);
        Task SendEmergencyRequestAsync(string expertId, object requestData);
        Task NotifyEmergencyRequestStatusChangedAsync(Guid requestId, object statusData);
        Task NotifyEmergencyRequestCreatedAsync(Guid requestId, Guid memberId, Guid expertId);
        Task NotifyEmergencyRequestAcceptedAsync(Guid requestId, Guid expertId);
        Task NotifyEmergencyRequestRejectedAsync(Guid requestId, Guid expertId);
    }
}
