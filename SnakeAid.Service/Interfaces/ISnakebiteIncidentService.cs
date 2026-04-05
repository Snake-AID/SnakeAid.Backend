using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.SnakebiteIncident;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakebiteIncidentService
    {
        Task<CreateIncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid userId);

        Task<DetailSnakebiteIncidentResponse> GetDetailIncidentAsync(Guid incidentId);

        Task<UpdateSymptomReportResponse> UpdateSymptomReportAsync(Guid incidentId, UpdateSymptomReportRequest request);

        Task<CreateIncidentResponse> CancelIncidentAsync(Guid incidentId, CancelIncidentRequest request);

        Task<CreateIncidentResponse> ConfirmIncidentAsync(Guid incidentId, Guid operatorId);

        Task<CreateIncidentResponse> DispatchIncidentAsync(Guid incidentId, Guid rescuerId, Guid operatorId);

        Task<CreateIncidentResponse> MarkIncidentFalseAlarmAsync(Guid incidentId, Guid operatorId, string? reason);

        Task<CreateIncidentResponse> ReportIncidentNoAnswerAsync(Guid incidentId, Guid operatorId, bool continueCalling, string? note);

        Task<AcceptRescueResponse> AcceptDispatchRequestAsync(Guid requestId, Guid rescuerId);

        Task<RejectRescueResponse> DeclineDispatchRequestAsync(Guid requestId, Guid rescuerId, string? reason);

        Task<RejectRescueResponse> CancelDispatchRequestAsync(Guid requestId, Guid operatorId);

        Task<List<DispatchRequestResponse>> GetDispatchRequestsAsync(Guid incidentId);

        // Debug: Get media info
        Task<object> GetMediaDebugInfoAsync(Guid incidentId);

        // Snake Identification
        Task<IdentifySnakeResponse> IdentifySnakeByAIAsync(Guid incidentId, Guid recognitionResultId);

        Task<IdentifySnakeResponse> IdentifySnakeByFilterAsync(Guid incidentId, IdentifyByFilterRequest request);

        Task<PagedData<ListSnakebiteIncidentResponse>> GetUserIncidentsAsync(Guid userId, SnakebiteIncidentStatus? status, int page, int pageSize);

        Task<PagedData<OperatorIncidentSummaryResponse>> GetActiveIncidentsAsync(
            IEnumerable<SnakebiteIncidentStatus>? statuses,
            DateTimeOffset? since,
            DateTimeOffset? until,
            int page,
            int pageSize);

        Task<PagedData<OperatorIncidentSummaryResponse>> GetAdminIncidentsAsync(
            IEnumerable<SnakebiteIncidentStatus>? statuses,
            DateTimeOffset? since,
            DateTimeOffset? until,
            int page,
            int pageSize);
    }
}
