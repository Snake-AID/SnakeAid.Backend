using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SnakeAid.Api.Hubs;
using SnakeAid.Api.Services;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers
{
    /// <summary>
    /// Demo controller for testing rescue flow with REAL services but DEMO data.
    /// NOTE: Session-based dispatch endpoints removed — dispatch is now handled by Operators.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RescueDemoController : ControllerBase
    {
        private readonly IHubContext<RescuerHub> _hubContext;
        private readonly ILogger<RescueDemoController> _logger;
        private readonly DemoDataSeeder _demoDataSeeder;
        private readonly ISnakebiteIncidentService _incidentService;
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        private static Guid? _currentDemoIncidentId = null;

        public RescueDemoController(
            IHubContext<RescuerHub> hubContext,
            ILogger<RescueDemoController> logger,
            DemoDataSeeder demoDataSeeder,
            ISnakebiteIncidentService incidentService,
            IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _hubContext = hubContext;
            _logger = logger;
            _demoDataSeeder = demoDataSeeder;
            _incidentService = incidentService;
            _unitOfWork = unitOfWork;
        }

        #region Demo Data Management

        [HttpPost("seed")]
        public async Task<IActionResult> SeedDemoData()
        {
            var success = await _demoDataSeeder.SeedDemoDataAsync();
            if (!success)
                return BadRequest("Failed to seed demo data. Check if data already exists or see logs.");
            return Ok(new
            {
                message = "Demo data seeded successfully",
                userId = DemoDataSeeder.DEMO_USER_ID,
                rescuers = new[]
                {
                    new { id = DemoDataSeeder.DEMO_RESCUER_A_ID, name = "Rescuer A" },
                    new { id = DemoDataSeeder.DEMO_RESCUER_B_ID, name = "Rescuer B" },
                    new { id = DemoDataSeeder.DEMO_RESCUER_C_ID, name = "Rescuer C" },
                    new { id = DemoDataSeeder.DEMO_RESCUER_D_ID, name = "Rescuer D" }
                }
            });
        }

        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupDemoData()
        {
            var success = await _demoDataSeeder.CleanupDemoDataAsync();
            if (!success)
                return BadRequest("Failed to cleanup demo data. See logs for details.");
            _currentDemoIncidentId = null;
            await _hubContext.Clients.All.SendAsync("DemoDataCleanedUp", new { Message = "Demo data cleaned up" });
            return Ok(new { message = "Demo data cleaned up successfully" });
        }

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

        #region User Actions

        /// <summary>Create SOS incident — Operator will receive and handle dispatch</summary>
        [HttpPost("incident/create")]
        public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentDto dto)
        {
            try
            {
                var demoStatus = await _demoDataSeeder.GetStatusAsync();
                if (!demoStatus.IsSeeded)
                    return BadRequest("Demo data not seeded. Call POST /api/rescuedemo/seed first.");

                var request = new CreateIncidentRequest { Lat = dto.Lat, Lng = dto.Lng };
                var response = await _incidentService.CreateIncidentAsync(request, DemoDataSeeder.DEMO_USER_ID);
                _currentDemoIncidentId = response.Id;

                await _hubContext.Clients.All.SendAsync("IncidentCreated", new
                {
                    IncidentId = response.Id,
                    UserId = DemoDataSeeder.DEMO_USER_ID,
                    Lat = dto.Lat,
                    Lng = dto.Lng,
                    Status = "Pending"
                });

                return Ok(new
                {
                    incidentId = response.Id,
                    status = response.Status.ToString(),
                    location = new { lat = dto.Lat, lng = dto.Lng },
                    message = "Incident created. An on-duty operator will contact you."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create demo incident");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("incident/{incidentId}/cancel")]
        public async Task<IActionResult> CancelIncident(Guid incidentId)
        {
            try
            {
                var response = await _incidentService.CancelIncidentAsync(incidentId);
                if (incidentId == _currentDemoIncidentId) _currentDemoIncidentId = null;
                return Ok(new { incidentId = response.Id, message = "Incident cancelled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel incident");
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Mission Actions

        /// <summary>Abort mission — incident returns to Confirmed for operator to re-dispatch</summary>
        [HttpPost("mission/{missionId}/abort")]
        public async Task<IActionResult> AbortMission(Guid missionId, [FromQuery] string? reason = "Rescuer cancelled")
        {
            try
            {
                var missionService = HttpContext.RequestServices.GetRequiredService<ISnakeRescueMissionService>();
                await missionService.RescuerAbortMissionAsync(missionId, reason ?? "Rescuer cancelled");
                return Ok(new { missionId, message = "Mission aborted. Incident reset to Confirmed for operator re-dispatch." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to abort mission");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("mission/{missionId}/status")]
        public async Task<IActionResult> UpdateMissionStatus(Guid missionId, [FromQuery] string status)
        {
            try
            {
                if (!Enum.TryParse<RescueMissionStatus>(status, true, out var missionStatus))
                    return BadRequest(new { error = "Invalid mission status" });
                var missionService = HttpContext.RequestServices.GetRequiredService<ISnakeRescueMissionService>();
                await missionService.UpdateMissionStatusAsync(missionId, missionStatus);
                return Ok(new { missionId, newStatus = status, message = $"Mission status updated to {status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update mission status");
                return BadRequest(new { error = ex.Message });
            }
        }

        #endregion

        #region Query APIs

        [HttpGet("incident/current")]
        public async Task<IActionResult> GetCurrentIncident()
        {
            if (_currentDemoIncidentId == null)
                return NotFound("No active demo incident");
            try
            {
                var incident = await _incidentService.GetDetailIncidentAsync(_currentDemoIncidentId.Value);
                return Ok(incident);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("rescuers")]
        public async Task<IActionResult> GetRescuers()
        {
            var rescuers = new[]
            {
                new { id = DemoDataSeeder.DEMO_RESCUER_A_ID, name = "Rescuer A" },
                new { id = DemoDataSeeder.DEMO_RESCUER_B_ID, name = "Rescuer B" },
                new { id = DemoDataSeeder.DEMO_RESCUER_C_ID, name = "Rescuer C" },
                new { id = DemoDataSeeder.DEMO_RESCUER_D_ID, name = "Rescuer D" }
            };

            var connectedRescuerIds = SignalRRescueNotificationService.ConnectedRescuers.Keys.ToHashSet();
            var rescuerProfiles = await _unitOfWork.GetRepository<RescuerProfile>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(r => rescuers.Select(x => x.id).Contains(r.AccountId))
                .Select(r => new { r.AccountId, r.IsOnline, r.LastLocationUpdate })
                .ToListAsync();

            var result = rescuers.Select(r => new
            {
                r.id,
                r.name,
                isConnected = connectedRescuerIds.Contains(r.id.ToString()),
                isOnlineInDb = rescuerProfiles.FirstOrDefault(p => p.AccountId == r.id)?.IsOnline ?? false,
                lastLocationUpdate = rescuerProfiles.FirstOrDefault(p => p.AccountId == r.id)?.LastLocationUpdate
            });

            return Ok(result);
        }

        #endregion
    }

    #region DTOs

    public class CreateIncidentDto
    {
        public double Lat { get; set; } = 15.5741;
        public double Lng { get; set; } = 108.4796;
        public string? SymptomsReport { get; set; }
    }

    #endregion
}
