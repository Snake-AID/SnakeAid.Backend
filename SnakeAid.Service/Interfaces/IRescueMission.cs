using System;
using System.Threading.Tasks;
using SnakeAid.Core.Requests.RescueMission;
using SnakeAid.Core.Responses.RescueMission;

namespace SnakeAid.Service.Interfaces
{
    public interface IRescueMissionService
    {
        /// <summary>
        /// Update rescue mission status
        /// When status is set to MissionCompleted, also updates the corresponding incident to Finished
        /// </summary>
        Task<RescueMissionStatusResponse> UpdateMissionStatusAsync(Guid missionId, UpdateRescueMissionStatusRequest request, Guid rescuerId);

        /// <summary>
        /// Get mission details
        /// </summary>
        Task<RescueMissionStatusResponse> GetMissionDetailsAsync(Guid missionId);
    }
}
