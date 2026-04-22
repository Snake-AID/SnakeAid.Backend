using SnakeAid.Core.Requests.SnakeCatchingMission;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakeCatchingMissionService
    {
        Task<SnakeCatchingMissionDetailResponse> StartMissionAsync(Guid rescuerId, Guid missionId, UpdateMissionStatusRequest request);
        Task<SnakeCatchingMissionDetailResponse> MarkAsArrivedAsync(Guid rescuerId, Guid missionId, UpdateMissionStatusRequest request);
        Task<SnakeCatchingMissionDetailResponse> CompleteMissionAsync(Guid rescuerId, Guid missionId, UpdateMissionStatusRequest request);
        Task<SnakeCatchingMissionDetailResponse> UncompleteMissionAsync(Guid rescuerId, Guid missionId, UncompleteSnakeCatchingMissionRequest request);
        Task<SnakeCatchingMissionDetailResponse> AbortMissionAsync(Guid userId, string userRole, Guid missionId, AbortSnakeCatchingMissionRequest request);
    }
}
