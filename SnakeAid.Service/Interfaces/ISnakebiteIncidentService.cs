using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.RescueRequestSession;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Responses.SnakebiteIncident;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakebiteIncidentService
    {
        Task<ApiResponse<CreateIncidentResponse>> CreateIncidentAsync(CreateIncidentRequest request, Guid userId);

        Task<ApiResponse<CreateIncidentResponse>> RaiseSessionRangeAsync(RaiseSessionRangeRequest request);

        Task<ApiResponse<UpdateSymptomReportResponse>> UpdateSymptomReportAsync(Guid incidentId, UpdateSymptomReportRequest request);


    }
}
