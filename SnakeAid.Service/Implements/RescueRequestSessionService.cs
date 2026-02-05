using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Responses.RescueRequestSession;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class RescueRequestSessionService : IRescueRequestSessionService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<RescueRequestSessionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IRescueMissionService _missionService;
        private readonly IRescueNotificationService _notificationService;
        private readonly ISessionTimeoutService _timeoutService;

        // Configuration constants (sau này lấy từ SystemSetting)
        private const int MAX_SESSIONS = 3;
        private const int REQUEST_TIMEOUT_SECONDS = 60;
        private static readonly int[] RADIUS_PROGRESSION = { 10, 20, 30 }; // km

        public RescueRequestSessionService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<RescueRequestSessionService> logger,
            IConfiguration configuration,
            IRescueMissionService missionService,
            IRescueNotificationService notificationService,
            ISessionTimeoutService timeoutService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _missionService = missionService;
            _notificationService = notificationService;
            _timeoutService = timeoutService;
        }

        /// Tạo session mới cho incident (initial hoặc expand)
        public async Task<RescueRequestSession> CreateSessionAsync(Guid incidentId, int sessionNumber, int radiusKm, SessionTrigger trigger)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Incident not found.");
                    }

                    // Validate max sessions
                    if (sessionNumber > MAX_SESSIONS)
                    {
                        incident.Status = SnakebiteIncidentStatus.NoRescuerFound;
                        _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
                        await _unitOfWork.CommitAsync();
                        throw new BadRequestException($"Maximum sessions ({MAX_SESSIONS}) reached. No rescuers found.");
                    }

                    var session = new RescueRequestSession
                    {
                        Id = Guid.NewGuid(),
                        IncidentId = incidentId,
                        SessionNumber = sessionNumber,
                        RadiusKm = radiusKm,
                        Status = SessionStatus.Active,
                        TriggerType = trigger,
                        RescuersPinged = 0,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Update incident tracking
                    incident.CurrentSessionNumber = sessionNumber;
                    incident.CurrentRadiusKm = radiusKm;
                    incident.LastSessionAt = DateTime.UtcNow;

                    await _unitOfWork.GetRepository<RescueRequestSession>().InsertAsync(session);
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
                    await _unitOfWork.CommitAsync();

                    // Schedule timeout monitoring for this session
                    var timeoutAt = DateTime.UtcNow.AddSeconds(REQUEST_TIMEOUT_SECONDS);
                    _timeoutService.ScheduleSessionTimeout(session.Id, timeoutAt);

                    _logger.LogInformation("Created session {SessionId} for incident {IncidentId}, radius {RadiusKm}km, trigger {Trigger}, timeout at {TimeoutAt}",
                        session.Id, incidentId, radiusKm, trigger, timeoutAt);

                    return session;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task BroadcastRequestsAsync(Guid sessionId)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var session = await _unitOfWork.GetRepository<RescueRequestSession>().FirstOrDefaultAsync(
                        predicate: s => s.Id == sessionId,
                        include: q => q.Include(s => s.Incident)
                    );

                    if (session == null)
                    {
                        throw new NotFoundException("Session not found.");
                    }

                    var incident = session.Incident;

                    // Query rescuers online trong radius bằng PostGIS
                    var rescuersInRadius = await GetRescuersInRadiusAsync(
                        incident.LocationCoordinates,
                        session.RadiusKm
                    );

                    if (!rescuersInRadius.Any())
                    {
                        _logger.LogWarning("No rescuers found in {RadiusKm}km radius for session {SessionId}",
                            session.RadiusKm, sessionId);
                        return session;
                    }

                    var expiredAt = DateTime.UtcNow.AddSeconds(REQUEST_TIMEOUT_SECONDS);
                    var requests = new List<RescuerRequest>();

                    // Build all RescuerRequest objects
                    foreach (var rescuer in rescuersInRadius)
                    {
                        requests.Add(new RescuerRequest
                        {
                            Id = Guid.NewGuid(),
                            SessionId = sessionId,
                            IncidentId = incident.Id,
                            RescuerId = rescuer.AccountId,
                            Status = RescueRequestStatus.Pending,
                            RequestSentAt = DateTime.UtcNow,
                            ExpiredAt = expiredAt,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Bulk insert all requests at once
                    await _unitOfWork.GetRepository<RescuerRequest>().InsertRangeAsync(requests);

                    // Create lookup dictionary for O(1) rescuer lookup (optimization)
                    var rescuerLookup = rescuersInRadius.ToDictionary(r => r.AccountId, r => r.AccountId.ToString());

                    // Push notifications to all connected rescuers (parallel execution)
                    var notificationTasks = requests.Select(request =>
                        SendRequestToRescuerAsync(
                            rescuerLookup[request.RescuerId], // O(1) lookup instead of O(n)
                            request,
                            session
                        )
                    );
                    await Task.WhenAll(notificationTasks);

                    // Update session tracking and commit
                    session.RescuersPinged = requests.Count;
                    _unitOfWork.GetRepository<RescueRequestSession>().Update(session);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation("Successfully broadcasted {Count} requests for session {SessionId}, radius {RadiusKm}km. " +
                        "All data committed to database.",
                        requests.Count, sessionId, session.RadiusKm);

                    return session;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting requests for session {SessionId}: {Message}", sessionId, ex.Message);
                throw;
            }
        }


        /// Query rescuers online trong radius bằng PostGIS
        private async Task<List<RescuerProfile>> GetRescuersInRadiusAsync(Point incidentLocation, int radiusKm)
        {
            // Convert km to meters for PostGIS distance calculation
            var radiusMeters = radiusKm * 1000;

            // Sử dụng CreateBaseQuery() theo pattern của GenericRepository
            var rescuers = await _unitOfWork.GetRepository<RescuerProfile>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(r => r.IsOnline)
                .Where(r => r.LastLocation != null)
                .Where(r => r.Type == RescuerType.Emergency || r.Type == RescuerType.Both)
                .Where(r => r.LastLocation!.Distance(incidentLocation) <= radiusMeters)
                .OrderBy(r => r.LastLocation!.Distance(incidentLocation))
                .ToListAsync();

            // Filter chỉ những rescuer đang connected tới hub (via notification service)
            var connectedRescuers = rescuers
                .Where(r => _notificationService.IsRescuerConnected(r.AccountId.ToString()))
                .ToList();

            return connectedRescuers;
        }

        /// <summary>
        /// Push request đến rescuer qua notification service
        /// </summary>
        private async Task SendRequestToRescuerAsync(string userId, RescuerRequest request, RescueRequestSession session)
        {
            await _notificationService.SendNewRequestAsync(userId, new
            {
                RequestId = request.Id,
                SessionId = session.Id,
                IncidentId = request.IncidentId,
                RadiusKm = session.RadiusKm,
                ExpiredAt = request.ExpiredAt,
                RequestSentAt = request.RequestSentAt
            });

            _logger.LogInformation("Sent request {RequestId} to rescuer {UserId}", request.Id, userId);
        }

        /// Handle timeout: Mark requests expired sau 60s, check nếu cần expand/create new session
        public async Task HandleSessionTimeoutAsync(Guid sessionId)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var session = await _unitOfWork.GetRepository<RescueRequestSession>().FirstOrDefaultAsync(
                        predicate: s => s.Id == sessionId,
                        include: q => q.Include(s => s.Requests).Include(s => s.Incident)
                    );

                    if (session == null)
                    {
                        throw new NotFoundException("Session not found.");
                    }

                    // Skip nếu session đã complete hoặc cancelled
                    if (session.Status != SessionStatus.Active)
                    {
                        _logger.LogInformation("Session {SessionId} already {Status}, skipping timeout handling",
                            sessionId, session.Status);
                        return session;
                    }

                    // Mark all pending requests as expired (bulk update for better performance)
                    var pendingRequests = session.Requests.Where(r => r.Status == RescueRequestStatus.Pending).ToList();
                    if (pendingRequests.Any())
                    {
                        var updateTime = DateTime.UtcNow;
                        foreach (var request in pendingRequests)
                        {
                            request.Status = RescueRequestStatus.Expired;
                            request.UpdatedAt = updateTime;
                        }
                        // Batch update
                        _unitOfWork.GetRepository<RescuerRequest>().UpdateRange(pendingRequests);
                    }

                    // Mark session as failed
                    session.Status = SessionStatus.Failed;
                    session.CompletedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<RescueRequestSession>().Update(session);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation("Session {SessionId} timed out, {Count} requests expired",
                        sessionId, pendingRequests.Count);

                    // Try expand and create new session
                    await TryExpandAndCreateNewSessionAsync(session.IncidentId);

                    return session;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling session timeout {SessionId}: {Message}", sessionId, ex.Message);
                throw;
            }
        }

        /// Accept request: Update RescuerRequest, tạo RescueMission, mark others Taken
        public async Task AcceptRequestAsync(Guid requestId, Guid rescuerId)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var request = await _unitOfWork.GetRepository<RescuerRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId && r.RescuerId == rescuerId,
                        include: q => q.Include(r => r.Session).ThenInclude(s => s.Requests)
                    );

                    if (request == null)
                    {
                        throw new NotFoundException($"Request not found or not assigned to this rescuer {rescuerId}.");
                    }

                    // Validate request status
                    if (request.Status != RescueRequestStatus.Pending)
                    {
                        throw new BadRequestException($"Cannot accept request with status: {request.Status}");
                    }

                    // Check if expired
                    if (DateTime.UtcNow > request.ExpiredAt)
                    {
                        request.Status = RescueRequestStatus.Expired;
                        _unitOfWork.GetRepository<RescuerRequest>().Update(request);
                        await _unitOfWork.CommitAsync();
                        throw new BadRequestException("Request has expired.");
                    }

                    // Check if session already completed (someone else accepted first)
                    if (request.Session.Status == SessionStatus.Completed)
                    {
                        request.Status = RescueRequestStatus.Taken;
                        _unitOfWork.GetRepository<RescuerRequest>().Update(request);
                        await _unitOfWork.CommitAsync();
                        throw new BadRequestException("Another rescuer has already accepted this incident.");
                    }

                    // Accept this request
                    request.Status = RescueRequestStatus.Accepted;
                    request.ResponseAt = DateTime.UtcNow;
                    request.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<RescuerRequest>().Update(request);

                    // Mark all other requests in the session as Taken
                    var otherRequests = request.Session.Requests.Where(r => r.Id != requestId && r.Status == RescueRequestStatus.Pending).ToList();
                    if (otherRequests.Any())
                    {
                        var updateTime = DateTime.UtcNow;
                        foreach (var otherRequest in otherRequests)
                        {
                            otherRequest.Status = RescueRequestStatus.Taken;
                            otherRequest.UpdatedAt = updateTime;
                        }
                        // Batch update
                        _unitOfWork.GetRepository<RescuerRequest>().UpdateRange(otherRequests);

                        // Notify other rescuers that request was taken (parallel notifications)
                        var notificationTasks = otherRequests.Select(otherRequest =>
                            NotifyRequestTakenAsync(otherRequest.RescuerId.ToString(), otherRequest.Id)
                        );
                        await Task.WhenAll(notificationTasks);
                    }

                    // Mark session as completed
                    request.Session.Status = SessionStatus.Completed;
                    request.Session.CompletedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<RescueRequestSession>().Update(request.Session);

                    // Cancel timeout monitoring since session is completed
                    _timeoutService.CancelSessionTimeout(request.Session.Id);

                    // Commit critical data first (request acceptance, session completion)
                    await _unitOfWork.CommitAsync();

                    // Create rescue mission (post-commit operation)
                    try
                    {
                        await _missionService.CreateMissionAsync(request.IncidentId, rescuerId, 0);
                        _logger.LogInformation("Mission created successfully for request {RequestId}", requestId);
                    }
                    catch (Exception missionEx)
                    {
                        // CRITICAL: Data already committed, log for manual intervention
                        _logger.LogError(missionEx,
                            "CRITICAL: Failed to create mission after accepting request {RequestId}. " +
                            "Data inconsistency detected. Manual intervention required. " +
                            "IncidentId: {IncidentId}, RescuerId: {RescuerId}",
                            requestId, request.IncidentId, rescuerId);

                        // TODO: Add to failed operations queue for retry
                        // TODO: Send alert to admin/ops team

                        // Re-throw để caller biết có issue (nhưng data đã committed)
                        throw new InvalidOperationException(
                            $"Request accepted but mission creation failed. RequestId: {requestId}",
                            missionEx);
                    }

                    _logger.LogInformation("Rescuer {RescuerId} accepted request {RequestId} for incident {IncidentId}",
                        rescuerId, requestId, request.IncidentId);

                    return request;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting request {RequestId}: {Message}", requestId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Notify rescuer that request was taken by someone else
        /// </summary>
        private async Task NotifyRequestTakenAsync(string userId, Guid requestId)
        {
            await _notificationService.NotifyRequestTakenAsync(userId, requestId);
        }

        /// <summary>
        /// Reject request: Update status
        /// </summary>
        public async Task RejectRequestAsync(Guid requestId)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var request = await _unitOfWork.GetRepository<RescuerRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId
                    );

                    if (request == null)
                    {
                        throw new NotFoundException("Request not found.");
                    }

                    if (request.Status != RescueRequestStatus.Pending)
                    {
                        throw new BadRequestException($"Cannot reject request with status: {request.Status}");
                    }

                    request.Status = RescueRequestStatus.Rejected;
                    request.ResponseAt = DateTime.UtcNow;
                    request.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.GetRepository<RescuerRequest>().Update(request);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation("Request {RequestId} rejected", requestId);

                    return request;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request {RequestId}: {Message}", requestId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Cancel session (user cancel incident)
        /// </summary>
        public async Task CancelSessionAsync(Guid sessionId)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var session = await _unitOfWork.GetRepository<RescueRequestSession>().FirstOrDefaultAsync(
                        predicate: s => s.Id == sessionId,
                        include: q => q.Include(s => s.Requests)
                    );

                    if (session == null)
                    {
                        throw new NotFoundException("Session not found.");
                    }

                    // Cancel all pending requests
                    foreach (var request in session.Requests.Where(r => r.Status == RescueRequestStatus.Pending))
                    {
                        request.Status = RescueRequestStatus.Cancelled;
                        request.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.GetRepository<RescuerRequest>().Update(request);

                        // Notify rescuer
                        await NotifyRequestCancelledAsync(request.RescuerId.ToString(), request.Id);
                    }

                    session.Status = SessionStatus.Cancelled;
                    session.CompletedAt = DateTime.UtcNow;

                    // Cancel timeout monitoring since session is cancelled
                    _timeoutService.CancelSessionTimeout(sessionId);

                    _unitOfWork.GetRepository<RescueRequestSession>().Update(session);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation("Session {SessionId} cancelled", sessionId);

                    return session;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling session {SessionId}: {Message}", sessionId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Notify rescuer that request was cancelled
        /// </summary>
        private async Task NotifyRequestCancelledAsync(string userId, Guid requestId)
        {
            await _notificationService.NotifyRequestCancelledAsync(userId, requestId);
        }

        /// <summary>
        /// Expand radius và tạo session mới nếu cần
        /// </summary>
        public async Task<bool> TryExpandAndCreateNewSessionAsync(Guid incidentId)
        {
            try
            {
                var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                    predicate: i => i.Id == incidentId
                );

                if (incident == null)
                {
                    throw new NotFoundException("Incident not found.");
                }

                // Check if incident is still pending
                if (incident.Status != SnakebiteIncidentStatus.Pending)
                {
                    _logger.LogInformation("Incident {IncidentId} is no longer pending ({Status}), skipping expand",
                        incidentId, incident.Status);
                    return false;
                }

                // Check if max sessions reached
                if (incident.CurrentSessionNumber >= MAX_SESSIONS)
                {
                    _logger.LogWarning("Max sessions ({MaxSessions}) reached for incident {IncidentId}, marking as NoRescuerFound",
                        MAX_SESSIONS, incidentId);

                    incident.Status = SnakebiteIncidentStatus.NoRescuerFound;
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
                    await _unitOfWork.CommitAsync();
                    return false;
                }

                // Get next radius from progression
                var nextSessionNumber = incident.CurrentSessionNumber + 1;
                var nextRadiusIndex = nextSessionNumber - 1;
                var nextRadius = nextRadiusIndex < RADIUS_PROGRESSION.Length
                    ? RADIUS_PROGRESSION[nextRadiusIndex]
                    : RADIUS_PROGRESSION[^1]; // Use last value if exceeded

                // Create new session
                var newSession = await CreateSessionAsync(
                    incidentId,
                    nextSessionNumber,
                    nextRadius,
                    SessionTrigger.RadiusExpanded
                );

                // Broadcast requests for new session
                await BroadcastRequestsAsync(newSession.Id);

                _logger.LogInformation("Expanded to session {SessionNumber} with radius {RadiusKm}km for incident {IncidentId}",
                    nextSessionNumber, nextRadius, incidentId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expanding session for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Start initial rescue session for incident (called from SnakebiteIncidentService)
        /// </summary>
        public async Task StartRescueSessionAsync(Guid incidentId)
        {
            var initialRadius = RADIUS_PROGRESSION[0]; // 10km
            var session = await CreateSessionAsync(incidentId, 1, initialRadius, SessionTrigger.Initial);
            await BroadcastRequestsAsync(session.Id);
        }

        /// <summary>
        /// Handle mission abort: Create new session with increased radius
        /// Called when rescuer aborts mission (after accepting request)
        /// </summary>
        public async Task HandleMissionAbortAsync(Guid incidentId)
        {
            try
            {
                var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                    predicate: i => i.Id == incidentId
                );

                if (incident == null)
                {
                    throw new NotFoundException("Incident not found.");
                }

                // Check if incident is still pending (should be reset by mission abort)
                if (incident.Status != SnakebiteIncidentStatus.Pending)
                {
                    _logger.LogWarning("Incident {IncidentId} is not pending ({Status}), cannot create new session after mission abort",
                        incidentId, incident.Status);
                    return;
                }

                // Check if max sessions reached
                if (incident.CurrentSessionNumber >= MAX_SESSIONS)
                {
                    _logger.LogWarning("Max sessions ({MaxSessions}) reached for incident {IncidentId}, marking as NoRescuerFound",
                        MAX_SESSIONS, incidentId);

                    incident.Status = SnakebiteIncidentStatus.NoRescuerFound;
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
                    await _unitOfWork.CommitAsync();
                    return;
                }

                // Get next radius from progression (increase radius due to mission cancellation)
                var nextSessionNumber = incident.CurrentSessionNumber + 1;
                var nextRadiusIndex = nextSessionNumber - 1;
                var nextRadius = nextRadiusIndex < RADIUS_PROGRESSION.Length
                    ? RADIUS_PROGRESSION[nextRadiusIndex]
                    : RADIUS_PROGRESSION[^1]; // Use last value if exceeded

                // Create new session with increased radius
                var newSession = await CreateSessionAsync(
                    incidentId,
                    nextSessionNumber,
                    nextRadius,
                    SessionTrigger.MissionCancelled
                );

                // Broadcast requests for new session
                await BroadcastRequestsAsync(newSession.Id);

                _logger.LogInformation("Created new session {SessionNumber} with radius {RadiusKm}km for incident {IncidentId} after mission abort",
                    nextSessionNumber, nextRadius, incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling mission abort for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }
    }
}
