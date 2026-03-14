using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IOperatorOnlineStatusService
    {
        Task SetOnDutyAsync(string userId);
        Task SetOffDutyAsync(string userId);
    }
}
