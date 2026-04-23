using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.RescueMission;
using SnakeAid.Core.Responses.RescueMission;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakeRescueMissionService
    {
        // Tạo mission khi rescuer accept request
        Task<RescueMission> CreateMissionAsync(Guid incidentId, Guid rescuerId);

        // Update mission status (e.g., EnRoute, Arrived)
        Task UpdateMissionStatusAsync(Guid missionId, RescueMissionStatus status);

        // Complete mission with evidence photos
        Task CompleteMissionAsync(Guid missionId, List<Guid> evidenceMediaIds, string? completionNotes);

        // User cancel mission: Set status to Cancelled, no new session
        // NOTE: This method is kept for backwards compatibility; prefer cancelling via the incident cancel endpoint.
        [Obsolete("Use ISnakebiteIncidentService.CancelIncidentAsync instead")]
        Task UserCancelMissionAsync(Guid missionId, string reason);

        // Rescuer abort mission: Set status to MissionAborted, create new session with increased radius
        Task RescuerAbortMissionAsync(Guid missionId, string reason);

        // Get mission by ID (domain entity)
        Task<RescueMission> GetMissionByIdAsync(Guid missionId);

        // Get detailed mission info with all related entities (for API response)
        Task<DetailRescueMissionResponse> GetMissionDetailAsync(Guid missionId);

        /// Get mission detail with calculated distance from rescuer location
        Task<DetailRescueMissionResponse> GetMissionDetailAsync(Guid missionId, double? rescuerLat, double? rescuerLng);

        Task<ICollection<ListRescueMissionResponse>> GetRescuerMissionListAsync(Guid rescuerId, RescueMissionStatus? status);

        Task<HospitalTransferPricingResponse> ReportHospitalTransferAsync(
            Guid missionId,
            Guid rescuerId,
            ReportHospitalTransferRequest request);

        Task<PagedData<AdminRescueMissionSummaryResponse>> GetAdminMissionListAsync(
            IEnumerable<RescueMissionStatus>? statuses,
            DateTimeOffset? since,
            DateTimeOffset? until,
            int page,
            int pageSize);
    }
}