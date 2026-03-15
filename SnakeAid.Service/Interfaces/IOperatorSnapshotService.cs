using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SnakeAid.Core.Responses.RescuerProfile;

namespace SnakeAid.Service.Interfaces
{
    public interface IOperatorSnapshotService
    {
        Task<OnDutyRescuerSnapshotResponse> GetOnDutyRescuersAsync(
            DateOnly? date,
            Guid? incidentId,
            bool onlyAvailable,
            double? maxDistanceKm);

        Task<List<BriefRescuerProfileResponse>> GetRescuerRegistryAsync();

        Task<BriefRescuerProfileResponse?> GetRescuerByIdAsync(Guid rescuerId);
    }
}