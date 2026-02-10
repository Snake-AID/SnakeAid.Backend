using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SnakeAid.Api.Hubs;
using SnakeAid.Api.Services;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.RescueRequestSession;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers
{
    /// <summary>
    /// Demo controller for testing rescue flow with REAL services but DEMO data
    /// Uses DemoDataSeeder to create test users/rescuers in actual database
    /// All service logic (session, broadcast, timeout, notifications) works exactly as production
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RescueDemoController : ControllerBase
    {
        private readonly IHubContext<RescuerHub> _hubContext;
        private readonly ILogger<RescueDemoController> _logger;
        private readonly DemoDataSeeder _demoDataSeeder;
        private readonly ISnakebiteIncidentService _incidentService;
        private readonly IRescueRequestSessionService _sessionService;
        private readonly ISessionTimeoutService _timeoutService;
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        // Track current demo incident for UI convenience
        private static Guid? _currentDemoIncidentId = null;

        public RescueDemoController(
            IHubContext<RescuerHub> hubContext,
            ILogger<RescueDemoController> logger,
            DemoDataSeeder demoDataSeeder,
            ISnakebiteIncidentService incidentService,
            IRescueRequestSessionService sessionService,
            ISessionTimeoutService timeoutService,
            IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _hubContext = hubContext;
            _logger = logger;
            _demoDataSeeder = demoDataSeeder;
            _incidentService = incidentService;
            _sessionService = sessionService;
            _timeoutService = timeoutService;
            _unitOfWork = unitOfWork;
        }

        #region Demo Data Management

        /// <summary>
        /// Seed demo users and rescuers into database
        /// </summary>
        [HttpPost("seed")]
        public async Task<IActionResult> SeedDemoData()
        {
            var success = await _demoDataSeeder.SeedDemoDataAsync();
            if (!success)
            {
                return BadRequest("Failed to seed demo data. Check if data already exists or see logs.");
            }

            return Ok(new
            {
                message = "Demo data seeded successfully",
                userId = DemoDataSeeder.DEMO_USER_ID,
                rescuers = new[]
                {
                    new { id = DemoDataSeeder.DEMO_RESCUER_A_ID, name = "Rescuer A - Quận 1" },
                    new { id = DemoDataSeeder.DEMO_RESCUER_B_ID, name = "Rescuer B - Quận 3" },
                    new { id = DemoDataSeeder.DEMO_RESCUER_C_ID, name = "Rescuer C - Quận 7" },
                    new { id = DemoDataSeeder.DEMO_RESCUER_D_ID, name = "Rescuer D - Tân Bình" }
                }
            });
        }

        /// <summary>
        /// Clean up all demo data (incidents, sessions, requests, missions, users)
        /// </summary>
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupDemoData()
        {
            var success = await _demoDataSeeder.CleanupDemoDataAsync();
            if (!success)
            {
                return BadRequest("Failed to cleanup demo data. See logs for details.");
            }

            _currentDemoIncidentId = null;

            await _hubContext.Clients.All.SendAsync("DemoDataCleanedUp", new { Message = "Demo data cleaned up" });

            return Ok(new { message = "Demo data cleaned up successfully" });
        }

        /// <summary>
        /// Get demo data status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetDemoStatus()
        {
            var status = await _demoDataSeeder.GetStatusAsync();
            return Ok(new
            {
                status,
                currentIncidentId = _currentDemoIncidentId,
                connectedRescuers = SignalRRescueNotificationService.ConnectedRescuers.Keys.ToList()
            });
        }

        #endregion

        #region User Actions (Using Real Services)

        /// <summary>
        /// Create incident + Start rescue session (matches real flow from SnakebiteIncidentController)
        /// This combines:
        /// 1. CreateIncidentAsync - Creates incident record
        /// 2. StartRescueAsync - Creates initial session + broadcasts to rescuers
        /// </summary>
        [HttpPost("incident/create")]
        public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentDto dto)
        {
            try
            {
                // Check if demo data is seeded
                var status = await _demoDataSeeder.GetStatusAsync();
                if (!status.IsSeeded)
                {
                    return BadRequest("Demo data not seeded. Call POST /api/rescuedemo/seed first.");
                }

                // Step 1: Create incident using REAL service (matches SnakebiteIncidentController line 55)
                var request = new CreateIncidentRequest
                {
                    Lat = dto.Lat,
                    Lng = dto.Lng
                };

                var response = await _incidentService.CreateIncidentAsync(request, DemoDataSeeder.DEMO_USER_ID);
                _currentDemoIncidentId = response.Id;

                _logger.LogInformation("Demo incident created: {IncidentId}", response.Id);

                // Step 2: Start rescue session and broadcast to rescuers (matches line 58)
                var rescueResult = await _incidentService.StartRescueAsync(response.Id);

                // Combine response data (matches line 61-65)
                response.SessionId = rescueResult.SessionId;
                response.SessionNumber = rescueResult.SessionNumber;
                response.RadiusKm = rescueResult.RadiusKm;
                response.RescuersPinged = rescueResult.RescuersPinged;

                _logger.LogInformation("Rescue session started: SessionId={SessionId}, Radius={Radius}km, Rescuers={Count}",
                    rescueResult.SessionId, rescueResult.RadiusKm, rescueResult.RescuersPinged);

                // Notify all clients
                await _hubContext.Clients.All.SendAsync("IncidentCreated", new
                {
                    IncidentId = response.Id,
                    SessionId = response.SessionId,
                    SessionNumber = response.SessionNumber,
                    RadiusKm = response.RadiusKm,
                    RescuersPinged = response.RescuersPinged,
                    UserId = DemoDataSeeder.DEMO_USER_ID,
                    Lat = dto.Lat,
                    Lng = dto.Lng,
                    Status = "Pending"
                });

                return Ok(new
                {
                    incidentId = response.Id,
                    sessionId = response.SessionId,
                    sessionNumber = response.SessionNumber,
                    radiusKm = response.RadiusKm,
                    rescuersPinged = response.RescuersPinged,
                    message = $"Incident created and rescue session started! {response.RescuersPinged} rescuers pinged within {response.RadiusKm}km radius."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create demo incident");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Trigger rescue using REAL session service (creates session, broadcasts to rescuers)
        /// This will:
        /// 1. Create initial session (sessionNumber=1, radius=5km)
        /// 2. Query rescuers within radius
        /// 3. Create RescuerRequest records
        /// 4. Broadcast via SignalR using IRescueNotificationService
        /// 5. Register 60s timeout in SessionTimeoutBackgroundService
        /// </summary>
        [HttpPost("incident/{incidentId}/trigger")]
        public async Task<IActionResult> TriggerRescue(Guid incidentId)
        {
            try
            {
                // Call REAL service
                var response = await _incidentService.TriggerRescueAsync(incidentId);

                _logger.LogInformation("Rescue triggered for incident {IncidentId}, session {SessionId}",
                    incidentId, response.SessionId);

                return Ok(new
                {
                    sessionId = response.SessionId,
                    sessionNumber = response.SessionNumber,
                    radiusKm = response.RadiusKm,
                    rescuersPinged = response.RescuersPinged,
                    message = $"Rescue triggered! {response.RescuersPinged} rescuers notified within {response.RadiusKm}km. Real background timeout (60s) is active."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger rescue");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Raise session range (expand radius) using REAL service
        /// This will:
        /// 1. Mark current session as Failed
        /// 2. Expire all pending requests
        /// 3. Create new session with expanded radius
        /// 4. Broadcast to new rescuers
        /// 5. Register new 60s timeout
        /// </summary>
        [HttpPost("incident/{incidentId}/raise-range")]
        public async Task<IActionResult> RaiseRange(Guid incidentId)
        {
            try
            {
                var response = await _incidentService.RaiseSessionRangeAsync(new RaiseSessionRangeRequest
                {
                    IncidentId = incidentId
                });

                _logger.LogInformation("Range raised for incident {IncidentId}, new session {SessionId}",
                    incidentId, response.SessionId);

                return Ok(new
                {
                    sessionId = response.SessionId,
                    sessionNumber = response.SessionNumber,
                    radiusKm = response.RadiusKm,
                    rescuersPinged = response.RescuersPinged,
                    message = $"Range expanded to {response.RadiusKm}km! {response.RescuersPinged} new rescuers notified."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to raise range");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cancel incident using REAL service
        /// This will:
        /// 1. Mark incident as Cancelled
        /// 2. Cancel all active sessions
        /// 3. Expire all pending requests
        /// 4. Cancel mission if exists
        /// 5. Notify all involved rescuers
        /// </summary>
        [HttpPost("incident/{incidentId}/cancel")]
        public async Task<IActionResult> CancelIncident(Guid incidentId)
        {
            try
            {
                var response = await _incidentService.CancelIncidentAsync(incidentId);

                _logger.LogInformation("Incident {IncidentId} cancelled", incidentId);

                if (incidentId == _currentDemoIncidentId)
                {
                    _currentDemoIncidentId = null;
                }

                return Ok(new
                {
                    incidentId = response.Id,
                    message = "Incident cancelled successfully. All sessions and requests terminated."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel incident");
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Rescuer Actions (Using Real Services)

        /// <summary>
        /// Rescuer accept request using REAL service
        /// This will:
        /// 1. Validate request is Pending and not expired
        /// 2. Mark request as Accepted
        /// 3. Mark all other requests in session as Taken
        /// 4. Mark session as Completed
        /// 5. Create RescueMission
        /// 6. Update incident status to Assigned
        /// 7. Notify all rescuers (accepted, taken)
        /// 8. Cancel timeout in background service
        /// </summary>
        [HttpPost("request/{requestId}/accept")]
        public async Task<IActionResult> AcceptRequest(Guid requestId, [FromQuery] Guid rescuerId)
        {
            try
            {
                // Call the REAL session service to accept request (not just validation)
                await _sessionService.AcceptRequestAsync(requestId, rescuerId);

                // Query created mission
                var request = await _unitOfWork.GetRepository<RescuerRequest>().FirstOrDefaultAsync(
                    predicate: r => r.Id == requestId
                );

                if (request == null)
                {
                    throw new NotFoundException("Request not found");
                }

                var mission = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                    predicate: m => m.IncidentId == request.IncidentId
                );

                _logger.LogInformation("Rescuer {RescuerId} accepted request {RequestId}, mission {MissionId}",
                    rescuerId, requestId, mission?.Id);

                return Ok(new
                {
                    requestId,
                    missionId = mission?.Id,
                    incidentId = request.IncidentId,
                    message = "Request accepted! Mission created. You have been assigned to this rescue."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to accept request");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Rescuer abort/cancel mission using REAL service
        /// This will:
        /// 1. Mark mission as MissionAborted
        /// 2. Reset incident to Pending
        /// 3. Create new session with increased radius
        /// 4. Exclude this rescuer from new session
        /// 5. Broadcast to other rescuers
        /// </summary>
        [HttpPost("mission/{missionId}/abort")]
        public async Task<IActionResult> AbortMission(Guid missionId, [FromQuery] string? reason = "Rescuer cancelled")
        {
            try
            {
                // Query mission to get rescuer and incident info
                // IMPORTANT: Use asNoTracking to avoid polluting DbContext with tracked entities
                // This ensures the mission service gets fresh data
                var mission = await _unitOfWork.GetRepository<RescueMission>().FirstOrDefaultAsync(
                    predicate: m => m.Id == missionId,
                    include: q => q.Include(m => m.Incident),
                    asNoTracking: true
                );

                if (mission == null)
                {
                    throw new NotFoundException("Mission not found");
                }

                var rescuerId = mission.RescuerId;
                var incidentId = mission.IncidentId;

                // Use MissionService to abort (it will call SessionService.HandleMissionAbortAsync)
                var missionService = HttpContext.RequestServices.GetRequiredService<IRescueMissionService>();
                await missionService.RescuerAbortMissionAsync(missionId, reason ?? "Rescuer cancelled");

                _logger.LogInformation("Rescuer {RescuerId} aborted mission {MissionId}, new session created for incident {IncidentId}",
                    rescuerId, missionId, incidentId);

                // Get updated incident info
                var updatedIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                    predicate: i => i.Id == incidentId,
                    include: q => q.Include(i => i.Sessions.OrderByDescending(s => s.SessionNumber).Take(1))
                );

                var latestSession = updatedIncident?.Sessions?.FirstOrDefault();

                return Ok(new
                {
                    message = "Mission aborted. Creating new session with expanded radius...",
                    incidentId,
                    newSession = new
                    {
                        sessionId = latestSession?.Id,
                        sessionNumber = latestSession?.SessionNumber,
                        radiusKm = latestSession?.RadiusKm,
                        rescuersPinged = latestSession?.RescuersPinged
                    },
                    excludedRescuerId = rescuerId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to abort mission");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update mission status (for demo: EnRoute, Arrived, Completed)
        /// </summary>
        [HttpPost("mission/{missionId}/status")]
        public async Task<IActionResult> UpdateMissionStatus(Guid missionId, [FromQuery] string status)
        {
            try
            {
                if (!Enum.TryParse<RescueMissionStatus>(status, true, out var missionStatus))
                {
                    return BadRequest(new { error = "Invalid mission status" });
                }

                var missionService = HttpContext.RequestServices.GetRequiredService<IRescueMissionService>();
                await missionService.UpdateMissionStatusAsync(missionId, missionStatus);

                _logger.LogInformation("Mission {MissionId} status updated to {Status}", missionId, status);

                return Ok(new
                {
                    missionId,
                    newStatus = status,
                    message = $"Mission status updated to {status}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update mission status");
                return BadRequest(new { error = ex.Message });
            }
        }

        #endregion

        #region Query APIs

        /// <summary>
        /// Get current demo incident details (from real database)
        /// </summary>
        [HttpGet("incident/current")]
        public async Task<IActionResult> GetCurrentIncident()
        {
            if (_currentDemoIncidentId == null)
            {
                return NotFound("No active demo incident");
            }

            try
            {
                var incident = await _incidentService.GetDetailIncidentAsync(_currentDemoIncidentId.Value);
                return Ok(incident);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get incident details");
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Get comprehensive monitoring data for current incident (incident + sessions + requests + mission)
        /// </summary>
        [HttpGet("incident/monitor")]
        public async Task<IActionResult> GetIncidentMonitoring()
        {
            if (_currentDemoIncidentId == null)
            {
                return Ok(new { hasIncident = false, message = "No active demo incident" });
            }

            try
            {
                // Get incident details
                var incident = await _incidentService.GetDetailIncidentAsync(_currentDemoIncidentId.Value);

                // Query all sessions for this incident
                var sessions = await _unitOfWork.GetRepository<RescueRequestSession>()
                    .CreateBaseQuery()
                    .Where(s => s.IncidentId == _currentDemoIncidentId.Value)
                    .OrderBy(s => s.SessionNumber)
                    .Select(s => new
                    {
                        s.Id,
                        s.SessionNumber,
                        s.RadiusKm,
                        s.Status,
                        s.TriggerType,
                        s.RescuersPinged,
                        s.CreatedAt
                    })
                    .ToListAsync();

                // Query all requests for this incident
                var requests = await _unitOfWork.GetRepository<RescuerRequest>()
                    .CreateBaseQuery()
                    .Where(r => r.IncidentId == _currentDemoIncidentId.Value)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        r.Id,
                        r.SessionId,
                        r.RescuerId,
                        RescuerName = r.Rescuer.Account.FullName,
                        r.Status,
                        r.RequestSentAt,
                        r.ExpiredAt,
                        r.ResponseAt
                    })
                    .ToListAsync();

                // Query mission if exists
                var mission = await _unitOfWork.GetRepository<RescueMission>()
                    .CreateBaseQuery()
                    .Where(m => m.IncidentId == _currentDemoIncidentId.Value)
                    .Select(m => new
                    {
                        m.Id,
                        m.RescuerId,
                        RescuerName = m.Rescuer.Account.FullName,
                        m.Status,
                        m.StartedAt,
                        m.ArrivedAt,
                        m.CompletedAt,
                        m.Price
                    })
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    hasIncident = true,
                    incident = new
                    {
                        incident.Id,
                        incident.Status,
                        incident.CurrentSessionNumber,
                        incident.CurrentRadiusKm,
                        incident.LastSessionAt,
                        incident.AssignedAt,
                        incident.AssignedRescuerId
                    },
                    sessions = sessions,
                    requests = requests.Select(r => new
                    {
                        r.Id,
                        r.SessionId,
                        r.RescuerId,
                        r.RescuerName,
                        r.Status,
                        r.RequestSentAt,
                        r.ExpiredAt,
                        r.ResponseAt,
                        SessionNumber = sessions.FirstOrDefault(s => s.Id == r.SessionId)?.SessionNumber
                    }),
                    mission = mission,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get incident monitoring data");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all demo rescuers with connection status
        /// </summary>
        [HttpGet("rescuers")]
        public IActionResult GetRescuers()
        {
            var rescuers = new[]
            {
                new { id = DemoDataSeeder.DEMO_RESCUER_A_ID, name = "Rescuer A - Quận 1", distanceKm = 4.5 },
                new { id = DemoDataSeeder.DEMO_RESCUER_B_ID, name = "Rescuer B - Quận 3", distanceKm = 3.2 },
                new { id = DemoDataSeeder.DEMO_RESCUER_C_ID, name = "Rescuer C - Quận 7", distanceKm = 8.0 },
                new { id = DemoDataSeeder.DEMO_RESCUER_D_ID, name = "Rescuer D - Tân Bình", distanceKm = 6.5 }
            };

            var result = rescuers.Select(r => new
            {
                r.id,
                r.name,
                r.distanceKm,
                isConnected = SignalRRescueNotificationService.ConnectedRescuers.ContainsKey(r.id.ToString())
            });

            return Ok(result);
        }

        /// <summary>
        /// Monitor background service sessions with real-time timeout tracking
        /// </summary>
        [HttpGet("sessions/monitor")]
        public IActionResult GetSessionMonitoring()
        {
            var monitoringInfo = _timeoutService.GetMonitoringInfo();
            var (totalSessions, expiredCount, pendingCount) = _timeoutService.GetQueueStatus();

            return Ok(new
            {
                summary = new
                {
                    totalTracked = totalSessions,
                    expired = expiredCount,
                    pending = pendingCount,
                    healthy = _timeoutService.IsHealthy()
                },
                sessions = monitoringInfo.Select(s => new
                {
                    sessionId = s.SessionId,
                    timeoutAt = s.TimeoutAt,
                    timeRemainingSeconds = (int)s.TimeRemaining.TotalSeconds,
                    isExpired = s.IsExpired,
                    status = s.IsExpired ? "Expired" :
                            s.TimeRemaining.TotalSeconds < 10 ? "Expiring Soon" : "Active"
                }),
                timestamp = DateTime.UtcNow
            });
        }

        #endregion
    }

    #region DTOs

    public class CreateIncidentDto
    {
        public double Lat { get; set; } = 10.762622;
        public double Lng { get; set; } = 106.660172;
        public string? SymptomsReport { get; set; }
    }

    #endregion
}
