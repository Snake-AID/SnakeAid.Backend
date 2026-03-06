using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IExpertEmergencyNotificationService
    {
        bool IsExpertConnected(string expertId);
        Task SendEmergencyRequestAsync(string expertId, object requestData);
        Task NotifyEmergencyRequestStatusChangedAsync(Guid requestId, object statusData);
    }
}
