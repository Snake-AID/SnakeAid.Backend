using SnakeAid.Core.Meta;
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
    }
}
