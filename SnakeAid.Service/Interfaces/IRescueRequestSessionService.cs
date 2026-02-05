using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Responses.RescueRequestSession;
using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface IRescueRequestSessionService
    {
        // Tạo session mới cho incident (initial hoặc expand)
        Task<RescueRequestSession> CreateSessionAsync(Guid incidentId, int sessionNumber, int radiusKm, SessionTrigger trigger);

        // Broadcast requests đến rescuers online trong radius (fanout qua SignalR)
        Task BroadcastRequestsAsync(Guid sessionId);

        // Handle timeout: Mark requests expired sau 60s, check nếu cần expand/create new session
        Task HandleSessionTimeoutAsync(Guid sessionId);

        // Accept request: Update RescuerRequest, tạo RescueMission, mark others Taken
        Task AcceptRequestAsync(Guid requestId, Guid rescuerId);

        // Reject request: Update status
        Task RejectRequestAsync(Guid requestId);

        // Cancel session (user cancel incident)
        Task CancelSessionAsync(Guid sessionId);

        // Expand radius và tạo session mới nếu cần (internal call từ HandleSessionTimeout)
        Task<bool> TryExpandAndCreateNewSessionAsync(Guid incidentId);

        // Start initial rescue session for incident
        Task StartRescueSessionAsync(Guid incidentId);

        // Handle mission abort: Create new session with increased radius
        Task HandleMissionAbortAsync(Guid incidentId);
    }
}
