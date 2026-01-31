using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.RescueRequestSession;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/incidents")]
    [ApiController]
    [Authorize]
    public class SnakebiteIncidentController : BaseController<SnakebiteIncidentController>
    {
        private readonly ISnakebiteIncidentService _incidentService;

        public SnakebiteIncidentController(
            ILogger<SnakebiteIncidentController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ISnakebiteIncidentService incidentService)
            : base(logger, httpContextAccessor, mapper)
        {
            _incidentService = incidentService;
        }

        /// <summary>
        /// Create first snakebite incident report and first rescue request session before dispatching rescuers
        /// </summary>
        [HttpPost("sos")]
        [SwaggerOperation(Summary = "Create Snakebite Incident", Description = "Report Snakebite Incident Emergency Rescue")]
        [SwaggerResponse(200, "Create successful", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(400, "Member profile not found")]
        [SwaggerResponse(422, "Validation error")]
        public async Task<IActionResult> CreateSnakebiteIncident([FromBody] CreateIncidentRequest request)
        {
            var userId = GetCurrentUserId();

            // Create incident and first rescue request session
            var result = await _incidentService.CreateIncidentAsync(request, userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Raise/expand the search range by creating a new rescue request session with larger radius
        /// </summary>
        [HttpPost("{incidentId}/raise-range")]
        [SwaggerOperation(Summary = "Raise Session Range", Description = "Expand search radius when no rescuers accept current session")]
        [SwaggerResponse(200, "Range expanded successfully", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(400, "Maximum sessions reached or invalid status")]
        [SwaggerResponse(404, "Incident not found")]
        public async Task<IActionResult> RaiseSessionRange(Guid incidentId)
        {
            var request = new RaiseSessionRangeRequest { IncidentId = incidentId };
            var result = await _incidentService.RaiseSessionRangeAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Update symptom report with time-based severity calculation
        /// </summary>
        [HttpPut("{incidentId}/symptoms-tracking")]
        [SwaggerOperation(Summary = "Update Symptom Report", Description = "Update incident symptoms and calculate severity based on elapsed time")]
        [SwaggerResponse(200, "Symptom report updated successfully", typeof(ApiResponse<UpdateSymptomReportResponse>))]
        [SwaggerResponse(404, "Incident not found")]
        [SwaggerResponse(422, "Validation error")]
        public async Task<IActionResult> UpdateSymptomReport(Guid incidentId, [FromBody] UpdateSymptomReportRequest request)
        {
            var result = await _incidentService.UpdateSymptomReportAsync(incidentId, request);
            return StatusCode(result.StatusCode, result);
        }
    }
}
