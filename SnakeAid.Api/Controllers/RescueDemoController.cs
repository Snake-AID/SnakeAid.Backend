using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SnakeAid.Api.Hubs;
using SnakeAid.Api.Services;
using SnakeAid.Core.Domains;
using System.Collections.Concurrent;

namespace SnakeAid.Api.Controllers
{
    /// <summary>
    /// Demo controller for testing rescue SignalR flow with mock data (no database required)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RescueDemoController : ControllerBase
    {
        private readonly IHubContext<RescuerHub> _hubContext;
        private readonly ILogger<RescueDemoController> _logger;

        // ====== MOCK DATA STORAGE (thay vì database) ======

        // Mock Users
        public static ConcurrentDictionary<Guid, MockUser> MockUsers { get; } = new()
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new MockUser
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Demo User",
                Role = "Member",
                Lat = 10.762622,  // HCM City center
                Lng = 106.660172
            }
        };

        // Mock Rescuers (4 rescuers at different locations around HCM)
        public static ConcurrentDictionary<Guid, MockRescuer> MockRescuers { get; } = new()
        {
            [Guid.Parse("22222222-2222-2222-2222-222222222221")] = new MockRescuer
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222221"),
                Name = "Rescuer A - Quận 1",
                Lat = 10.7700,
                Lng = 106.6980,
                IsOnline = false,
                DistanceFromUserKm = 4.5
            },
            [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new MockRescuer
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Rescuer B - Quận 3",
                Lat = 10.7831,
                Lng = 106.6859,
                IsOnline = false,
                DistanceFromUserKm = 3.2
            },
            [Guid.Parse("22222222-2222-2222-2222-222222222223")] = new MockRescuer
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222223"),
                Name = "Rescuer C - Quận 7",
                Lat = 10.7295,
                Lng = 106.7215,
                IsOnline = false,
                DistanceFromUserKm = 8.0
            },
            [Guid.Parse("22222222-2222-2222-2222-222222222224")] = new MockRescuer
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222224"),
                Name = "Rescuer D - Tân Bình",
                Lat = 10.8053,
                Lng = 106.6482,
                IsOnline = false,
                DistanceFromUserKm = 6.5
            }
        };

        // Mock Incidents
        public static ConcurrentDictionary<Guid, MockIncident> MockIncidents { get; } = new();

        // Mock Sessions
        public static ConcurrentDictionary<Guid, MockSession> MockSessions { get; } = new();

        // Mock Requests (per rescuer per session)
        public static ConcurrentDictionary<Guid, MockRescuerRequest> MockRequests { get; } = new();

        // Mock Missions
        public static ConcurrentDictionary<Guid, MockMission> MockMissions { get; } = new();

        // Session timeout timers
        private static ConcurrentDictionary<Guid, System.Timers.Timer> SessionTimers { get; } = new();

        public RescueDemoController(IHubContext<RescuerHub> hubContext, ILogger<RescueDemoController> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        #region User Actions

        /// <summary>
        /// User tạo incident mới
        /// </summary>
        [HttpPost("incident/create")]
        public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentDto dto)
        {
            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var incident = new MockIncident
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Lat = dto.Lat,
                Lng = dto.Lng,
                Status = "Pending",
                CurrentSessionNumber = 0,
                CurrentRadiusKm = 0,
                CreatedAt = DateTime.UtcNow
            };

            MockIncidents[incident.Id] = incident;

            _logger.LogInformation("Created mock incident {IncidentId}", incident.Id);

            // Notify all connected clients about new incident
            await _hubContext.Clients.All.SendAsync("IncidentCreated", incident);

            return Ok(new { incident, message = "Incident created successfully. Ready to trigger rescue." });
        }

        /// <summary>
        /// User trigger rescue session (bắt đầu tìm rescuer)
        /// </summary>
        [HttpPost("incident/{incidentId}/trigger")]
        public async Task<IActionResult> TriggerRescue(Guid incidentId)
        {
            if (!MockIncidents.TryGetValue(incidentId, out var incident))
            {
                return NotFound("Incident not found");
            }

            if (incident.Status != "Pending")
            {
                return BadRequest($"Cannot trigger rescue for incident with status: {incident.Status}");
            }

            // Create first session
            var session = await CreateNewSessionAsync(incident, 1, 5, "Initial");

            // Start timeout timer (60 seconds)
            StartSessionTimer(session.Id, 60);

            return Ok(new { session, message = "Rescue triggered. Broadcasting to nearby rescuers..." });
        }

        /// <summary>
        /// User cancel incident
        /// </summary>
        [HttpPost("incident/{incidentId}/cancel")]
        public async Task<IActionResult> CancelIncident(Guid incidentId)
        {
            if (!MockIncidents.TryGetValue(incidentId, out var incident))
            {
                return NotFound("Incident not found");
            }

            if (incident.Status != "Pending" && incident.Status != "Assigned")
            {
                return BadRequest($"Cannot cancel incident with status: {incident.Status}");
            }

            incident.Status = "Cancelled";

            // Cancel all active sessions and requests
            var activeSessions = MockSessions.Values.Where(s => s.IncidentId == incidentId && s.Status == "Active");
            foreach (var session in activeSessions)
            {
                await CancelSessionAsync(session.Id);
            }

            // Cancel mission if exists
            var mission = MockMissions.Values.FirstOrDefault(m => m.IncidentId == incidentId);
            if (mission != null && (mission.Status == "Preparing" || mission.Status == "EnRoute"))
            {
                mission.Status = "Cancelled";
                mission.UpdatedAt = DateTime.UtcNow;

                // Notify rescuer
                var rescuerId = mission.RescuerId.ToString();
                if (SignalRRescueNotificationService.ConnectedRescuers.ContainsKey(rescuerId))
                {
                    await _hubContext.Clients.Client(SignalRRescueNotificationService.ConnectedRescuers[rescuerId])
                        .SendAsync("MissionCancelled", new { MissionId = mission.Id, Reason = "User cancelled incident" });
                }
            }

            await _hubContext.Clients.All.SendAsync("IncidentCancelled", incident);

            return Ok(new { incident, message = "Incident cancelled" });
        }

        /// <summary>
        /// User raise session range (mở rộng bán kính tìm kiếm)
        /// </summary>
        [HttpPost("incident/{incidentId}/raise-range")]
        public async Task<IActionResult> RaiseSessionRange(Guid incidentId)
        {
            if (!MockIncidents.TryGetValue(incidentId, out var incident))
            {
                return NotFound("Incident not found");
            }

            if (incident.Status != "Pending")
            {
                return BadRequest($"Cannot raise range for incident with status: {incident.Status}");
            }

            // Close current session as Failed
            var currentSession = MockSessions.Values
                .FirstOrDefault(s => s.IncidentId == incidentId && s.SessionNumber == incident.CurrentSessionNumber);

            if (currentSession != null)
            {
                // Stop timer
                StopSessionTimer(currentSession.Id);

                currentSession.Status = "Failed";
                currentSession.CompletedAt = DateTime.UtcNow;

                // Mark all pending requests as expired
                var pendingRequests = MockRequests.Values.Where(r => r.SessionId == currentSession.Id && r.Status == "Pending");
                foreach (var req in pendingRequests)
                {
                    req.Status = "Expired";
                    req.UpdatedAt = DateTime.UtcNow;

                    // Notify rescuer
                    await NotifyRescuerAsync(req.RescuerId.ToString(), "RequestExpired", new { RequestId = req.Id });
                }
            }

            // Check max sessions
            if (incident.CurrentSessionNumber >= 3)
            {
                incident.Status = "NoRescuerFound";
                await _hubContext.Clients.All.SendAsync("IncidentNoRescuerFound", incident);
                return Ok(new { incident, message = "Maximum session range expansions reached. No rescuers found." });
            }

            // Calculate new radius
            int newRadius = incident.CurrentRadiusKm switch
            {
                5 => 7,
                7 => 10,
                _ => incident.CurrentRadiusKm + 5
            };

            // Create new session
            var newSession = await CreateNewSessionAsync(incident, incident.CurrentSessionNumber + 1, newRadius, "RadiusExpanded");

            // Start timeout timer
            StartSessionTimer(newSession.Id, 60);

            return Ok(new { session = newSession, message = $"Range expanded to {newRadius}km" });
        }

        #endregion

        #region Rescuer Actions

        /// <summary>
        /// Rescuer accept request
        /// </summary>
        [HttpPost("request/{requestId}/accept")]
        public async Task<IActionResult> AcceptRequest(Guid requestId, [FromQuery] Guid rescuerId)
        {
            if (!MockRequests.TryGetValue(requestId, out var request))
            {
                return NotFound("Request not found");
            }

            if (request.RescuerId != rescuerId)
            {
                return BadRequest("This request is not assigned to you");
            }

            if (request.Status != "Pending")
            {
                return BadRequest($"Cannot accept request with status: {request.Status}");
            }

            // Check if expired
            if (DateTime.UtcNow > request.ExpiredAt)
            {
                request.Status = "Expired";
                request.UpdatedAt = DateTime.UtcNow;
                return BadRequest("Request has expired");
            }

            // Check if session already completed
            var session = MockSessions[request.SessionId];
            if (session.Status == "Completed")
            {
                request.Status = "Taken";
                request.UpdatedAt = DateTime.UtcNow;
                return BadRequest("Another rescuer has already accepted this incident");
            }

            // Accept this request
            request.Status = "Accepted";
            request.ResponseAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            // Stop session timer
            StopSessionTimer(session.Id);

            // Mark all other requests in session as Taken
            var otherRequests = MockRequests.Values.Where(r => r.SessionId == session.Id && r.Id != requestId && r.Status == "Pending").ToList();
            foreach (var otherReq in otherRequests)
            {
                otherReq.Status = "Taken";
                otherReq.UpdatedAt = DateTime.UtcNow;

                // Notify other rescuers
                await NotifyRescuerAsync(otherReq.RescuerId.ToString(), "RequestTaken", new
                {
                    RequestId = otherReq.Id,
                    Message = "This request has been taken by another rescuer."
                });
            }

            // Mark session as completed
            session.Status = "Completed";
            session.CompletedAt = DateTime.UtcNow;

            // Create mission
            var incident = MockIncidents[request.IncidentId];
            incident.Status = "Assigned";
            incident.AssignedRescuerId = rescuerId;
            incident.AssignedAt = DateTime.UtcNow;

            var mission = new MockMission
            {
                Id = Guid.NewGuid(),
                IncidentId = request.IncidentId,
                RescuerId = rescuerId,
                Status = "Preparing",
                CreatedAt = DateTime.UtcNow
            };
            MockMissions[mission.Id] = mission;

            // Notify caller
            await NotifyRescuerAsync(rescuerId.ToString(), "RequestAccepted", new
            {
                RequestId = requestId,
                MissionId = mission.Id,
                Message = "Request accepted! You have been assigned to this rescue mission."
            });

            // Notify user
            await _hubContext.Clients.All.SendAsync("IncidentAssigned", new
            {
                IncidentId = incident.Id,
                RescuerId = rescuerId,
                RescuerName = MockRescuers[rescuerId].Name,
                MissionId = mission.Id
            });

            _logger.LogInformation("Rescuer {RescuerId} accepted request {RequestId}, mission {MissionId} created",
                rescuerId, requestId, mission.Id);

            return Ok(new { request, mission, message = "Request accepted successfully" });
        }

        /// <summary>
        /// Rescuer reject request
        /// </summary>
        [HttpPost("request/{requestId}/reject")]
        public async Task<IActionResult> RejectRequest(Guid requestId)
        {
            if (!MockRequests.TryGetValue(requestId, out var request))
            {
                return NotFound("Request not found");
            }

            if (request.Status != "Pending")
            {
                return BadRequest($"Cannot reject request with status: {request.Status}");
            }

            request.Status = "Rejected";
            request.ResponseAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            await NotifyRescuerAsync(request.RescuerId.ToString(), "RequestRejected", new { RequestId = requestId });

            return Ok(new { request, message = "Request rejected" });
        }

        /// <summary>
        /// Cancel mission by rescuer
        /// </summary>
        [HttpPost("mission/{missionId}/cancel")]
        public async Task<IActionResult> CancelMission(Guid missionId, [FromQuery] string reason = "Rescuer cancelled")
        {
            if (!MockMissions.TryGetValue(missionId, out var mission))
            {
                return NotFound("Mission not found");
            }

            if (mission.Status != "Preparing" && mission.Status != "EnRoute")
            {
                return BadRequest($"Cannot cancel mission with status: {mission.Status}");
            }

            mission.Status = "Cancelled";
            mission.CancellationReason = reason;
            mission.UpdatedAt = DateTime.UtcNow;

            var incident = MockIncidents[mission.IncidentId];

            // Reset incident to Pending for retry
            incident.Status = "Pending";
            incident.AssignedRescuerId = null;
            incident.AssignedAt = null;

            // Create new session triggered by mission cancellation
            var newSession = await CreateNewSessionAsync(incident, incident.CurrentSessionNumber + 1, incident.CurrentRadiusKm, "MissionCancelled");

            // Start timeout timer
            StartSessionTimer(newSession.Id, 60);

            // Notify user
            await _hubContext.Clients.All.SendAsync("MissionCancelledByRescuer", new
            {
                MissionId = missionId,
                IncidentId = incident.Id,
                Reason = reason,
                NewSessionId = newSession.Id,
                Message = "Rescuer cancelled. Looking for another rescuer..."
            });

            return Ok(new { mission, newSession, message = "Mission cancelled, new session created" });
        }

        /// <summary>
        /// Update mission status
        /// </summary>
        [HttpPost("mission/{missionId}/status")]
        public async Task<IActionResult> UpdateMissionStatus(Guid missionId, [FromQuery] string status)
        {
            if (!MockMissions.TryGetValue(missionId, out var mission))
            {
                return NotFound("Mission not found");
            }

            var oldStatus = mission.Status;
            mission.Status = status;
            mission.UpdatedAt = DateTime.UtcNow;

            switch (status)
            {
                case "EnRoute":
                    mission.StartedAt = DateTime.UtcNow;
                    break;
                case "RescuerArrived":
                    mission.ArrivedAt = DateTime.UtcNow;
                    break;
                case "MissionCompleted":
                    mission.CompletedAt = DateTime.UtcNow;
                    var incident = MockIncidents[mission.IncidentId];
                    incident.Status = "Finished";
                    break;
            }

            await _hubContext.Clients.All.SendAsync("MissionStatusUpdated", new
            {
                MissionId = missionId,
                OldStatus = oldStatus,
                NewStatus = status,
                Mission = mission
            });

            return Ok(new { mission, message = $"Mission status updated to {status}" });
        }

        #endregion

        #region Query APIs

        /// <summary>
        /// Get all mock data state
        /// </summary>
        [HttpGet("state")]
        public IActionResult GetState()
        {
            return Ok(new
            {
                users = MockUsers.Values.ToList(),
                rescuers = MockRescuers.Values.Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Lat,
                    r.Lng,
                    r.IsOnline,
                    r.DistanceFromUserKm,
                    IsConnected = SignalRRescueNotificationService.ConnectedRescuers.ContainsKey(r.Id.ToString())
                }).ToList(),
                incidents = MockIncidents.Values.ToList(),
                sessions = MockSessions.Values.ToList(),
                requests = MockRequests.Values.ToList(),
                missions = MockMissions.Values.ToList(),
                connectedRescuers = SignalRRescueNotificationService.ConnectedRescuers.Keys.ToList()
            });
        }

        /// <summary>
        /// Get incident details
        /// </summary>
        [HttpGet("incident/{incidentId}")]
        public IActionResult GetIncident(Guid incidentId)
        {
            if (!MockIncidents.TryGetValue(incidentId, out var incident))
            {
                return NotFound("Incident not found");
            }

            var sessions = MockSessions.Values.Where(s => s.IncidentId == incidentId).OrderBy(s => s.SessionNumber).ToList();
            var requests = MockRequests.Values.Where(r => r.IncidentId == incidentId).ToList();
            var mission = MockMissions.Values.FirstOrDefault(m => m.IncidentId == incidentId);

            return Ok(new { incident, sessions, requests, mission });
        }

        /// <summary>
        /// Get requests for a rescuer
        /// </summary>
        [HttpGet("rescuer/{rescuerId}/requests")]
        public IActionResult GetRescuerRequests(Guid rescuerId)
        {
            var requests = MockRequests.Values.Where(r => r.RescuerId == rescuerId).ToList();
            return Ok(requests);
        }

        /// <summary>
        /// Reset all mock data
        /// </summary>
        [HttpPost("reset")]
        public async Task<IActionResult> ResetMockData()
        {
            // Stop all timers
            foreach (var timer in SessionTimers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }
            SessionTimers.Clear();

            MockIncidents.Clear();
            MockSessions.Clear();
            MockRequests.Clear();
            MockMissions.Clear();

            // Reset rescuer online status
            foreach (var rescuer in MockRescuers.Values)
            {
                rescuer.IsOnline = SignalRRescueNotificationService.ConnectedRescuers.ContainsKey(rescuer.Id.ToString());
            }

            await _hubContext.Clients.All.SendAsync("DataReset", new { Message = "All mock data has been reset" });

            return Ok(new { message = "Mock data reset successfully" });
        }

        /// <summary>
        /// Simulate session timeout manually (for testing)
        /// </summary>
        [HttpPost("session/{sessionId}/timeout")]
        public async Task<IActionResult> SimulateTimeout(Guid sessionId)
        {
            await HandleSessionTimeoutAsync(sessionId);
            return Ok(new { message = "Session timeout handled" });
        }

        #endregion

        #region Private Helper Methods

        private async Task<MockSession> CreateNewSessionAsync(MockIncident incident, int sessionNumber, int radiusKm, string trigger)
        {
            var session = new MockSession
            {
                Id = Guid.NewGuid(),
                IncidentId = incident.Id,
                SessionNumber = sessionNumber,
                RadiusKm = radiusKm,
                Status = "Active",
                TriggerType = trigger,
                RescuersPinged = 0,
                CreatedAt = DateTime.UtcNow
            };

            MockSessions[session.Id] = session;

            // Update incident
            incident.CurrentSessionNumber = sessionNumber;
            incident.CurrentRadiusKm = radiusKm;
            incident.LastSessionAt = DateTime.UtcNow;

            // Find and ping connected rescuers within radius
            var connectedRescuers = MockRescuers.Values
                .Where(r => SignalRRescueNotificationService.ConnectedRescuers.ContainsKey(r.Id.ToString()))
                .Where(r => r.DistanceFromUserKm <= radiusKm)
                .ToList();

            session.RescuersPinged = connectedRescuers.Count;

            var expiredAt = DateTime.UtcNow.AddSeconds(60);

            foreach (var rescuer in connectedRescuers)
            {
                var request = new MockRescuerRequest
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.Id,
                    IncidentId = incident.Id,
                    RescuerId = rescuer.Id,
                    Status = "Pending",
                    RequestSentAt = DateTime.UtcNow,
                    ExpiredAt = expiredAt,
                    CreatedAt = DateTime.UtcNow
                };

                MockRequests[request.Id] = request;

                // Send request to rescuer via SignalR
                await NotifyRescuerAsync(rescuer.Id.ToString(), "NewRescueRequest", new
                {
                    RequestId = request.Id,
                    SessionId = session.Id,
                    IncidentId = incident.Id,
                    IncidentLat = incident.Lat,
                    IncidentLng = incident.Lng,
                    RadiusKm = radiusKm,
                    SessionNumber = sessionNumber,
                    ExpiredAt = expiredAt,
                    RequestSentAt = request.RequestSentAt,
                    TimeoutSeconds = 60
                });
            }

            // Notify all about session creation
            await _hubContext.Clients.All.SendAsync("SessionCreated", new
            {
                Session = session,
                RescuersPinged = connectedRescuers.Count,
                RescuerNames = connectedRescuers.Select(r => r.Name).ToList()
            });

            _logger.LogInformation("Session {SessionId} created with {Count} rescuers pinged in {RadiusKm}km",
                session.Id, connectedRescuers.Count, radiusKm);

            return session;
        }

        private async Task CancelSessionAsync(Guid sessionId)
        {
            if (!MockSessions.TryGetValue(sessionId, out var session))
                return;

            StopSessionTimer(sessionId);

            session.Status = "Cancelled";
            session.CompletedAt = DateTime.UtcNow;

            // Cancel all pending requests
            var pendingRequests = MockRequests.Values.Where(r => r.SessionId == sessionId && r.Status == "Pending");
            foreach (var req in pendingRequests)
            {
                req.Status = "Cancelled";
                req.UpdatedAt = DateTime.UtcNow;

                await NotifyRescuerAsync(req.RescuerId.ToString(), "RequestCancelled", new
                {
                    RequestId = req.Id,
                    Message = "Request cancelled by user"
                });
            }

            await _hubContext.Clients.All.SendAsync("SessionCancelled", session);
        }

        private async Task HandleSessionTimeoutAsync(Guid sessionId)
        {
            if (!MockSessions.TryGetValue(sessionId, out var session))
                return;

            if (session.Status != "Active")
                return;

            // Mark all pending requests as expired
            var pendingRequests = MockRequests.Values.Where(r => r.SessionId == sessionId && r.Status == "Pending").ToList();
            foreach (var req in pendingRequests)
            {
                req.Status = "Expired";
                req.UpdatedAt = DateTime.UtcNow;

                await NotifyRescuerAsync(req.RescuerId.ToString(), "RequestExpired", new
                {
                    RequestId = req.Id,
                    Message = "Request has expired"
                });
            }

            session.Status = "Failed";
            session.CompletedAt = DateTime.UtcNow;

            await _hubContext.Clients.All.SendAsync("SessionTimeout", new
            {
                SessionId = sessionId,
                ExpiredRequests = pendingRequests.Count,
                Message = "Session timed out. All pending requests expired."
            });

            // Try to expand and create new session
            var incident = MockIncidents[session.IncidentId];

            if (incident.Status == "Pending" && incident.CurrentSessionNumber < 3)
            {
                int nextRadius = session.RadiusKm switch
                {
                    5 => 7,
                    7 => 10,
                    _ => session.RadiusKm + 5
                };

                var newSession = await CreateNewSessionAsync(incident, incident.CurrentSessionNumber + 1, nextRadius, "RadiusExpanded");
                StartSessionTimer(newSession.Id, 60);

                await _hubContext.Clients.All.SendAsync("SessionAutoExpanded", new
                {
                    OldSessionId = sessionId,
                    NewSession = newSession,
                    Message = $"Auto-expanded to {nextRadius}km"
                });
            }
            else if (incident.CurrentSessionNumber >= 3)
            {
                incident.Status = "NoRescuerFound";
                await _hubContext.Clients.All.SendAsync("IncidentNoRescuerFound", incident);
            }
        }

        private void StartSessionTimer(Guid sessionId, int seconds)
        {
            var timer = new System.Timers.Timer(seconds * 1000);
            timer.Elapsed += async (sender, e) =>
            {
                timer.Stop();
                await HandleSessionTimeoutAsync(sessionId);
            };
            timer.AutoReset = false;
            timer.Start();

            SessionTimers[sessionId] = timer;
            _logger.LogInformation("Started {Seconds}s timer for session {SessionId}", seconds, sessionId);
        }

        private void StopSessionTimer(Guid sessionId)
        {
            if (SessionTimers.TryRemove(sessionId, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                _logger.LogInformation("Stopped timer for session {SessionId}", sessionId);
            }
        }

        private async Task NotifyRescuerAsync(string rescuerId, string method, object data)
        {
            if (SignalRRescueNotificationService.ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
            {
                try
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync(method, data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending {Method} to rescuer {RescuerId}", method, rescuerId);
                }
            }
        }

        #endregion
    }

    #region Mock DTOs

    public class CreateIncidentDto
    {
        public double Lat { get; set; } = 10.762622;
        public double Lng { get; set; } = 106.660172;
    }

    public class MockUser
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public class MockRescuer
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public bool IsOnline { get; set; }
        public double DistanceFromUserKm { get; set; }
    }

    public class MockIncident
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Status { get; set; } = "Pending";
        public int CurrentSessionNumber { get; set; }
        public int CurrentRadiusKm { get; set; }
        public DateTime? LastSessionAt { get; set; }
        public Guid? AssignedRescuerId { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MockSession
    {
        public Guid Id { get; set; }
        public Guid IncidentId { get; set; }
        public int SessionNumber { get; set; }
        public int RadiusKm { get; set; }
        public string Status { get; set; } = "Active";
        public string TriggerType { get; set; } = "Initial";
        public int RescuersPinged { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class MockRescuerRequest
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public Guid IncidentId { get; set; }
        public Guid RescuerId { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime RequestSentAt { get; set; }
        public DateTime? ResponseAt { get; set; }
        public DateTime ExpiredAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class MockMission
    {
        public Guid Id { get; set; }
        public Guid IncidentId { get; set; }
        public Guid RescuerId { get; set; }
        public string Status { get; set; } = "Preparing";
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    #endregion
}
