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
        private readonly IRescueNotificationService _notificationService;
        private readonly ISessionTimeoutService _timeoutService;

        // Configuration constants (sau này lấy từ SystemSetting)
        private const int MAX_SESSIONS = 3;
        private const int REQUEST_TIMEOUT_SECONDS = 60;
        private const decimal DEFAULT_RESCUE_PRICE = 500000m;
        private static readonly int[] RADIUS_PROGRESSION = { 10, 20, 30 }; // km

        public RescueRequestSessionService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<RescueRequestSessionService> logger,
            IConfiguration configuration,
            IRescueNotificationService notificationService,
            ISessionTimeoutService timeoutService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _notificationService = notificationService;
            _timeoutService = timeoutService;
        }

        /// Tạo session mới cho incident (initial hoặc expand) - Internal version without transaction
        private async Task<RescueRequestSession> CreateSessionInternalAsync(Guid incidentId, int sessionNumber, int radiusKm, SessionTrigger trigger)
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

            // Schedule timeout monitoring for this session
            var timeoutAt = DateTime.UtcNow.AddSeconds(REQUEST_TIMEOUT_SECONDS);
            _timeoutService.ScheduleSessionTimeout(session.Id, timeoutAt);

            _logger.LogInformation("Created session {SessionId} for incident {IncidentId}, radius {RadiusKm}km, trigger {Trigger}, timeout at {TimeoutAt}",
                session.Id, incidentId, radiusKm, trigger, timeoutAt);

            return session;
        }

        /// Tạo session mới cho incident (initial hoặc expand) - Public version with transaction
        public async Task<RescueRequestSession> CreateSessionAsync(Guid incidentId, int sessionNumber, int radiusKm, SessionTrigger trigger)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    return await CreateSessionInternalAsync(incidentId, sessionNumber, radiusKm, trigger);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        /// Broadcast requests to rescuers - Internal version without transaction (accepts session object)
        private async Task<RescueRequestSession> BroadcastRequestsInternalAsync(RescueRequestSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            // Ensure incident is loaded
            if (session.Incident == null)
            {
                var sessionIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                    predicate: i => i.Id == session.IncidentId
                );
                if (sessionIncident == null)
                {
                    throw new NotFoundException("Incident not found.");
                }
                session.Incident = sessionIncident;
            }

            // Validate session is still active before broadcasting
            if (session.Status != SessionStatus.Active)
            {
                _logger.LogWarning("Cannot broadcast requests for session {SessionId} with status {Status}",
                    session.Id, session.Status);
                throw new BadRequestException($"Cannot broadcast requests for session with status: {session.Status}");
            }

            var incident = session.Incident;

            // Query rescuers online trong radius bằng PostGIS
            _logger.LogInformation("Querying rescuers for session {SessionId}, incident {IncidentId}, radius {RadiusKm}km",
                session.Id, session.IncidentId, session.RadiusKm);

            var rescuersInRadius = await GetRescuersInRadiusAsync(
                incident.LocationCoordinates,
                session.RadiusKm
            );

            _logger.LogInformation("Found {Count} rescuers in {RadiusKm}km radius for session {SessionId}",
                rescuersInRadius.Count, session.RadiusKm, session.Id);

            if (!rescuersInRadius.Any())
            {
                _logger.LogWarning("No rescuers found in {RadiusKm}km radius for session {SessionId}",
                    session.RadiusKm, session.Id);
                return session;
            }

            // Get rescuer IDs for filtering
            var rescuerIds = rescuersInRadius.Select(r => r.AccountId).ToList();

            _logger.LogInformation("Checking for existing pending requests (excluding current incident {IncidentId}) for {Count} rescuers",
                session.IncidentId, rescuerIds.Count);

            // Query existing pending requests for these rescuers to avoid double-ping
            // IMPORTANT: Exclude pending requests for the CURRENT incident (allow re-ping in new session)
            // Only skip rescuers who have pending requests for OTHER incidents
            var rescuersWithPending = await _unitOfWork.GetRepository<RescuerRequest>()
                .CreateBaseQuery()
                .Where(r => rescuerIds.Contains(r.RescuerId)
                    && r.Status == RescueRequestStatus.Pending
                    && r.IncidentId != session.IncidentId) // Exclude current incident's requests
                .Select(r => r.RescuerId)
                .Distinct()
                .ToListAsync();

            _logger.LogInformation("Found {Count} rescuers with pending requests for OTHER incidents",
                rescuersWithPending.Count);

            // Query rescuers who have aborted missions for THIS incident
            // These rescuers should NOT receive requests again for the same incident
            var rescuersWhoAborted = await _unitOfWork.GetRepository<RescueMission>()
                .CreateBaseQuery()
                .Where(m => m.IncidentId == session.IncidentId
                    && m.Status == RescueMissionStatus.MissionAborted)
                .Select(m => m.RescuerId)
                .Distinct()
                .ToListAsync();

            _logger.LogInformation("Found {Count} rescuers who previously aborted missions for incident {IncidentId}: {RescuerIds}",
                rescuersWhoAborted.Count, session.IncidentId,
                string.Join(", ", rescuersWhoAborted));

            var rescuersWithPendingSet = new HashSet<Guid>(rescuersWithPending);
            var rescuersWhoAbortedSet = new HashSet<Guid>(rescuersWhoAborted);

            var expiredAt = DateTime.UtcNow.AddSeconds(REQUEST_TIMEOUT_SECONDS);
            var requests = new List<RescuerRequest>();

            // Only send to rescuers without any pending request AND who haven't aborted this incident
            foreach (var rescuer in rescuersInRadius)
            {
                var rescuerId = rescuer.AccountId;

                // Skip if rescuer already has a pending request for OTHER incidents (preserve existing session)
                if (rescuersWithPendingSet.Contains(rescuerId))
                {
                    _logger.LogDebug(
                        "Skipping rescuer {RescuerId} - already has pending request for ANOTHER incident (preserving existing session)",
                        rescuerId);
                    continue;
                }

                // Skip if rescuer previously aborted mission for THIS incident
                if (rescuersWhoAbortedSet.Contains(rescuerId))
                {
                    _logger.LogInformation("❌ Excluding rescuer {RescuerId} - previously aborted mission for incident {IncidentId}",
                        rescuerId, session.IncidentId);
                    continue;
                }

                requests.Add(new RescuerRequest
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.Id,
                    IncidentId = incident.Id,
                    RescuerId = rescuerId,
                    Status = RescueRequestStatus.Pending,
                    RequestSentAt = DateTime.UtcNow,
                    ExpiredAt = expiredAt,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Created {RequestCount} requests for session {SessionId}. " +
                "Skipped: {SkippedPending} with pending requests + {SkippedAborted} who aborted this incident",
                requests.Count, session.Id,
                rescuersWithPending.Count,
                rescuersWhoAborted.Count);

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

            // Update session tracking
            session.RescuersPinged = requests.Count;
            _unitOfWork.GetRepository<RescueRequestSession>().Update(session);

            _logger.LogInformation("Successfully broadcasted {Count} requests for session {SessionId}, radius {RadiusKm}km. " +
                "All data committed to database.",
                requests.Count, session.Id, session.RadiusKm);

            return session;
        }

        /// Broadcast requests to rescuers - Public version with transaction
        public async Task BroadcastRequestsAsync(Guid sessionId)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Query session from database
                    var session = await _unitOfWork.GetRepository<RescueRequestSession>().FirstOrDefaultAsync(
                        predicate: s => s.Id == sessionId,
                        include: q => q.Include(s => s.Incident)
                    );

                    if (session == null)
                    {
                        throw new NotFoundException("Session not found.");
                    }

                    return await BroadcastRequestsInternalAsync(session);
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

            _logger.LogDebug("Querying rescuers: radius {RadiusKm}km ({RadiusMeters}m), location ({Lng}, {Lat})",
                radiusKm, radiusMeters, incidentLocation.X, incidentLocation.Y);

            // First, get ALL rescuer profiles to see their IsOnline status
            var allRescuers = await _unitOfWork.GetRepository<RescuerProfile>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(r => r.LastLocation != null)
                .Where(r => r.Type == RescuerType.Emergency || r.Type == RescuerType.Both)
                .Select(r => new { r.AccountId, r.IsOnline })
                .ToListAsync();

            _logger.LogWarning("DEBUG: All rescuer profiles in DB - Total: {Count}, Online: {OnlineCount}, IDs: {RescuerStatuses}",
                allRescuers.Count,
                allRescuers.Count(r => r.IsOnline),
                string.Join(", ", allRescuers.Select(r => $"{r.AccountId}:{(r.IsOnline ? "ONLINE" : "OFFLINE")}")));

            // Sử dụng CreateBaseQuery() theo pattern của GenericRepository
            var rescuers = await _unitOfWork.GetRepository<RescuerProfile>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(r => r.IsOnline)
                .Where(r => r.LastLocation != null)
                .Where(r => r.Type == RescuerType.Emergency || r.Type == RescuerType.Both)
                .Where(r => r.LastLocation!.Distance(incidentLocation) <= radiusMeters)
                .OrderBy(r => r.LastLocation!.Distance(incidentLocation))
                .ToListAsync();

            _logger.LogInformation("Database query found {Count} rescuers (IsOnline=true, within {RadiusKm}km)",
                rescuers.Count, radiusKm);

            // Check SignalR connection status
            var connectedCount = rescuers.Count(r => _notificationService.IsRescuerConnected(r.AccountId.ToString()));
            _logger.LogWarning("DEBUG: SignalR connections - {ConnectedCount} of {TotalCount} rescuers are connected: {ConnectedIds}",
                connectedCount,
                rescuers.Count,
                string.Join(", ", rescuers.Select(r => $"{r.AccountId}:{(_notificationService.IsRescuerConnected(r.AccountId.ToString()) ? "CONNECTED" : "NOT-CONNECTED")}")));

            // Filter chỉ những rescuer đang connected tới hub (via notification service)
            var connectedRescuers = rescuers
                .Where(r => _notificationService.IsRescuerConnected(r.AccountId.ToString()))
                .ToList();

            _logger.LogInformation("After SignalR connection filter: {ConnectedCount} of {TotalCount} rescuers are connected to hub",
                connectedRescuers.Count, rescuers.Count);

            return connectedRescuers;
        }


        /// Push request đến rescuer qua notification service

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
                var session = await _unitOfWork.GetRepository<RescueRequestSession>().FirstOrDefaultAsync(
                    predicate: s => s.Id == sessionId,
                    include: q => q.Include(s => s.Requests).Include(s => s.Incident)
                );

                if (session == null)
                {
                    // Session may have been deleted (incident cancelled, already processed, etc.)
                    // This is normal for background cleanup - just log and skip
                    _logger.LogWarning("Session {SessionId} not found during timeout handling - may have been cancelled or already processed",
                        sessionId);
                    return;
                }

                // Skip nếu session đã complete hoặc cancelled
                if (session.Status != SessionStatus.Active)
                {
                    _logger.LogInformation("Session {SessionId} already {Status}, skipping timeout handling",
                        sessionId, session.Status);
                    return;
                }

                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
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

                    _logger.LogInformation("Session {SessionId} timed out, {Count} requests expired",
                        sessionId, pendingRequests.Count);

                    // Notify all rescuers that their requests have expired (parallel notifications)
                    if (pendingRequests.Any())
                    {
                        var expiredNotificationTasks = pendingRequests.Select(request =>
                            NotifyRequestExpiredAsync(request.RescuerId.ToString(), request.Id)
                        ).ToArray();
                        await Task.WhenAll(expiredNotificationTasks);
                    }

                    // Try expand and create new session
                    _logger.LogInformation("Attempting to expand session for incident {IncidentId} (from session {SessionId})",
                        session.IncidentId, sessionId);

                    await TryExpandAndCreateNewSessionAsync(session.IncidentId);

                    return session;
                });
            }
            catch (NotFoundException ex)
            {
                // Session or incident was deleted - this is expected behavior when incidents are cancelled
                _logger.LogInformation("Session timeout processing skipped: {Message}", ex.Message);
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
                    // Load request with session only (no circular reference)
                    var request = await _unitOfWork.GetRepository<RescuerRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId && r.RescuerId == rescuerId,
                        include: q => q.Include(r => r.Session),
                        asNoTracking: false
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
                        throw new BadRequestException("Request has expired.");
                    }

                    // Check if session already completed (someone else accepted first)
                    if (request.Session.Status == SessionStatus.Completed)
                    {
                        request.Status = RescueRequestStatus.Taken;
                        _unitOfWork.GetRepository<RescuerRequest>().Update(request);
                        throw new BadRequestException("Another rescuer has already accepted this incident.");
                    }

                    // Accept this request
                    request.Status = RescueRequestStatus.Accepted;
                    request.ResponseAt = DateTime.UtcNow;
                    request.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<RescuerRequest>().Update(request);

                    // Query other pending requests in the same session separately (no circular reference)
                    var otherRequests = await _unitOfWork.GetRepository<RescuerRequest>().GetListAsync(
                        predicate: r => r.SessionId == request.SessionId && r.Id != requestId && r.Status == RescueRequestStatus.Pending,
                        asNoTracking: false
                    );
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

                    // Create rescue mission inline (to avoid circular dependency with IRescueMissionService)
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == request.IncidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Incident not found.");
                    }

                    // Verify rescuer profile exists
                    var rescuer = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                        predicate: r => r.AccountId == rescuerId
                    );

                    if (rescuer == null)
                    {
                        throw new NotFoundException("Rescuer not found.");
                    }

                    // Check for existing active missions only (allow multiple missions per incident for retry scenarios)
                    var existingActiveMission = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                        predicate: m => m.IncidentId == request.IncidentId &&
                            (m.Status == RescueMissionStatus.Preparing ||
                             m.Status == RescueMissionStatus.EnRoute ||
                             m.Status == RescueMissionStatus.RescuerArrived)
                    );

                    if (existingActiveMission != null)
                    {
                        throw new BadRequestException($"Active mission {existingActiveMission.Id} already exists for this incident.");
                    }

                    // Create new mission
                    var mission = new RescueMission
                    {
                        Id = Guid.NewGuid(),
                        IncidentId = request.IncidentId,
                        RescuerId = rescuerId,
                        Status = RescueMissionStatus.Preparing,
                        Price = DEFAULT_RESCUE_PRICE,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Update incident status
                    incident.Status = SnakebiteIncidentStatus.Assigned;
                    incident.AssignedRescuerId = rescuerId;
                    incident.AssignedAt = DateTime.UtcNow;

                    await _unitOfWork.GetRepository<RescueMission>().InsertAsync(mission);
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    _logger.LogInformation("Rescuer {RescuerId} accepted request {RequestId} for incident {IncidentId}, mission {MissionId} created",
                        rescuerId, requestId, request.IncidentId, mission.Id);

                    return request;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting request {RequestId}: {Message}", requestId, ex.Message);
                throw;
            }
        }


        /// Notify rescuer that request was taken by someone else

        private async Task NotifyRequestTakenAsync(string userId, Guid requestId)
        {
            await _notificationService.NotifyRequestTakenAsync(userId, requestId);
        }


        /// Notify rescuer that request has expired
        private async Task NotifyRequestExpiredAsync(string userId, Guid requestId)
        {
            await _notificationService.NotifyRequestExpiredAsync(userId, requestId);
        }


        /// Cancel session (user cancel incident)
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


        /// Notify rescuer that request was cancelled
        private async Task NotifyRequestCancelledAsync(string userId, Guid requestId)
        {
            await _notificationService.NotifyRequestCancelledAsync(userId, requestId);
        }


        /// Expand radius và tạo session mới nếu cần
        public async Task<bool> TryExpandAndCreateNewSessionAsync(Guid incidentId)
        {
            try
            {
                var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                    predicate: i => i.Id == incidentId
                );

                if (incident == null)
                {
                    // Incident may have been deleted (user cancelled, etc.)
                    _logger.LogWarning("Incident {IncidentId} not found during session expansion - may have been cancelled",
                        incidentId);
                    return false;
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
                    return false;
                }

                // Get next radius from progression
                var nextSessionNumber = incident.CurrentSessionNumber + 1;
                var nextRadiusIndex = nextSessionNumber - 1;
                var nextRadius = nextRadiusIndex < RADIUS_PROGRESSION.Length
                    ? RADIUS_PROGRESSION[nextRadiusIndex]
                    : RADIUS_PROGRESSION[^1]; // Use last value if exceeded

                // Create new session (use internal version - already in transaction)
                var newSession = await CreateSessionInternalAsync(
                    incidentId,
                    nextSessionNumber,
                    nextRadius,
                    SessionTrigger.RadiusExpanded
                );

                // Broadcast requests for new session (pass session object - already in transaction)
                await BroadcastRequestsInternalAsync(newSession);

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


        /// Start initial rescue session for incident (called from SnakebiteIncidentService)
        public async Task StartRescueSessionAsync(Guid incidentId)
        {
            var initialRadius = RADIUS_PROGRESSION[0]; // 10km
            var session = await CreateSessionAsync(incidentId, 1, initialRadius, SessionTrigger.Initial);
            await BroadcastRequestsAsync(session.Id);
        }


        /// Handle mission abort: Create new session with increased radius
        /// Called when rescuer aborts mission (after accepting request)
        public async Task HandleMissionAbortAsync(Guid incidentId)
        {
            try
            {
                var incident = await _unitOfWork.GetRepository<SnakebiteIncident>()
                    .FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId,
                        asNoTracking: false
                    );

                if (incident == null)
                {
                    throw new NotFoundException("Incident not found.");
                }

                // Diagnostic: Check entity state to verify it's truly detached
                var entityState = _unitOfWork.Context.Entry(incident).State;
                _logger.LogInformation("Incident {IncidentId} status after mission abort: Status={Status} (raw: {StatusInt}), CurrentSession={Session}, EntityState={EntityState}",
                    incidentId, incident.Status, (int)incident.Status, incident.CurrentSessionNumber, entityState);

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

                    // Wrap in transaction to ensure status update is committed
                    await _unitOfWork.ExecuteInTransactionAsync(async () =>
                    {
                        incident.Status = SnakebiteIncidentStatus.NoRescuerFound;
                        _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);
                        return await Task.FromResult(0);
                    });
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
