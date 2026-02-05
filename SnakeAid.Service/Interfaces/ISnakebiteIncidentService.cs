using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.RescueRequestSession;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.SnakebiteIncident;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakebiteIncidentService
    {
        Task<CreateIncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid userId);

        Task<DetailSnakebiteIncidentReposne> GetDetailIncidentAsync(Guid incidentId);

        Task<CreateIncidentResponse> RaiseSessionRangeAsync(RaiseSessionRangeRequest request);
        Task<CreateIncidentResponse> RaiseSessionRangeAsync(RaiseSessionRangeRequest request);

        Task<UpdateSymptomReportResponse> UpdateSymptomReportAsync(Guid incidentId, UpdateSymptomReportRequest request);
        Task<UpdateSymptomReportResponse> UpdateSymptomReportAsync(Guid incidentId, UpdateSymptomReportRequest request);

        Task<CreateIncidentResponse> CancelIncidentAsync(Guid incidentId);

        // Trigger rescue: Tạo session initial, broadcast requests
        Task<TriggerRescueResponse> TriggerRescueAsync(Guid incidentId);

        // Start rescue session for existing incident (separated from CreateIncident)
        Task<TriggerRescueResponse> StartRescueAsync(Guid incidentId);

        // Handle rescuer accept (từ SignalR callback)
        Task<AcceptRescueResponse> AcceptRescueAsync(Guid requestId, Guid rescuerId);

        // Handle rescuer reject (từ SignalR callback)
        Task<RejectRescueResponse> RejectRescueAsync(Guid requestId);
    }
}
