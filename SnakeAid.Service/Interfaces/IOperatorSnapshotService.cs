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
    }
}