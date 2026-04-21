using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IExpertOnlineStatusService
    {
        Task<bool> SetOnlineAsync(string userId);
        Task<bool> SetOfflineAsync(string userId);
    }
}
