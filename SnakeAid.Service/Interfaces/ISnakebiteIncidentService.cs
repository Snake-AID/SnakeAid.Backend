using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.RescueRequestSession;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.SnakebiteIncident;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakebiteIncidentService
    {
        Task<CreateIncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid userId);

        Task<CreateIncidentResponse> RaiseSessionRangeAsync(RaiseSessionRangeRequest request);

        Task<UpdateSymptomReportResponse> UpdateSymptomReportAsync(Guid incidentId, UpdateSymptomReportRequest request);

        Task<CreateIncidentResponse> CancelIncidentAsync(Guid incidentId);
    }
}
