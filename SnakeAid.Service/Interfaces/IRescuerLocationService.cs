using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IRescuerLocationService
    {
        Task UpdateLocationAsync(Guid rescuerId, double latitude, double longitude, double? accuracy, double? speed, double? heading);
    }
}
