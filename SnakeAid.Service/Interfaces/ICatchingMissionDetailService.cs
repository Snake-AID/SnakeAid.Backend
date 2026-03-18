using SnakeAid.Core.Requests.SnakeCatchingMission;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface ICatchingMissionDetailService
    {
        Task<CatchingMissionDetailResponse> CreateCatchingMissionDetailAsync(CreateCatchingMissionDetailRequest request);
        Task<CatchingMissionDetailResponse> UpdateCatchingMissionDetailAsync(Guid id, UpdateCatchingMissionDetailRequest request);
        Task DeleteCatchingMissionDetailAsync(Guid id);
    }
}
