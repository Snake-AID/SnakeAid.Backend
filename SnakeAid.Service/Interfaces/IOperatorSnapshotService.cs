using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SnakeAid.Core.Responses.RescuerProfile;

namespace SnakeAid.Service.Interfaces
{
    public interface IOperatorSnapshotService
    {
        Task<List<BriefRescuerProfileResponse>> GetOnlineRescuersAsync();

        Task<OnDutyRescuerSnapshotResponse> GetOnDutyRescuersAsync(
            DateOnly? date,
            Guid? incidentId,
            Guid? catchingRequestId,
            bool onlyAvailable,
            double? maxDistanceKm);

        Task<OnDutyRescuerSnapshotResponse> GetOnDutyRescuersForIncidentAsync(
            DateOnly? date,
            Guid incidentId,
            bool onlyAvailable,
            double? maxDistanceKm);

        Task<OnDutyRescuerSnapshotResponse> GetOnDutyRescuersForCatchingRequestAsync(
            DateOnly? date,
            Guid catchingRequestId,
            bool onlyAvailable,
            double? maxDistanceKm);

        Task<OffDutyRescuerSnapshotResponse> GetOffDutyRescuersAsync(
            Guid? incidentId,
            Guid? catchingRequestId,
            double? maxDistanceKm);

        Task<List<BriefRescuerProfileResponse>> GetRescuerRegistryAsync();

        Task<BriefRescuerProfileResponse?> GetRescuerByIdAsync(Guid rescuerId);
    }
}