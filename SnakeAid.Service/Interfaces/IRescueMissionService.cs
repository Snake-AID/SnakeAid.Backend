using SnakeAid.Core.Domains;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IRescueMissionService
    {
        // Tạo mission khi rescuer accept request
        Task<RescueMission> CreateMissionAsync(Guid incidentId, Guid rescuerId, decimal price);

        // Update mission status (e.g., EnRoute, Completed)
        Task UpdateMissionStatusAsync(Guid missionId, RescueMissionStatus status);

        // User cancel mission: Set status to Cancelled, no new session
        Task UserCancelMissionAsync(Guid missionId, string reason);

        // Rescuer abort mission: Set status to MissionAborted, create new session with increased radius
        Task RescuerAbortMissionAsync(Guid missionId, string reason);

    }
}