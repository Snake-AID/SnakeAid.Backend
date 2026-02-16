using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IRescuerOnlineStatusService
    {
        Task SetOnlineAsync(string userId);
        Task SetOfflineAsync(string userId);
    }
}