using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.RescueRequestSession;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/incidents")]
    [ApiController]
    // [Authorize]
    public class SnakebiteIncidentController : BaseController<SnakebiteIncidentController>
    {
        private readonly ISnakebiteIncidentService _incidentService;
        private readonly IRescueRequestSessionService _sessionService;

        public SnakebiteIncidentController(
            ILogger<SnakebiteIncidentController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ISnakebiteIncidentService incidentService,
            IRescueRequestSessionService sessionService)
            : base(logger, httpContextAccessor, mapper)
        {
            _incidentService = incidentService;
            _sessionService = sessionService;
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

            // Step 1: Create incident first
            var result = await _incidentService.CreateIncidentAsync(request, userId);

            // Step 2: Start rescue session and broadcast to rescuers
            var rescueResult = await _incidentService.StartRescueAsync(result.Id);

            // Combine response data
            result.SessionId = rescueResult.SessionId;
            result.SessionNumber = rescueResult.SessionNumber;
            result.RadiusKm = rescueResult.RadiusKm;
            result.RescuersPinged = rescueResult.RescuersPinged;

            var response = ApiResponseBuilder.BuildSuccessResponse(result,
                "Snakebite Incident created and rescue session started! Broadcasting to nearby rescuers.");
            return StatusCode(response.StatusCode, response);
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
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, $"Session range expanded successfully. New radius: {result.CurrentRadiusKm}km"));
        }

        /// <summary>
        /// Get detailed information about a specific snakebite incident
        /// </summary>
        [HttpGet("{incidentId}")]
        [SwaggerOperation(Summary = "Get Incident Detail", Description = "Retrieve detailed information about a snakebite incident including user, rescuer, sessions, and media")]
        [SwaggerResponse(200, "Incident details retrieved successfully", typeof(ApiResponse<DetailSnakebiteIncidentResponse>))]
        [SwaggerResponse(404, "Incident not found")]
        public async Task<IActionResult> GetIncidentDetail(Guid incidentId)
        {
            var result = await _incidentService.GetDetailIncidentAsync(incidentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Incident details retrieved successfully!"));
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
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Symptom report updated successfully!"));
        }

        [HttpPut("{incidentId}/cancel")]
        [SwaggerOperation(Summary = "Cancel Incident", Description = "Cancel a snakebite incident if it is in Pending or Assigned status")]
        [SwaggerResponse(200, "Incident cancelled successfully", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(404, "Incident not found")]
        [SwaggerResponse(422, "Validation error")]
        public async Task<IActionResult> CancelIncident(Guid incidentId)
        {
            var result = await _incidentService.CancelIncidentAsync(incidentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Incident cancelled successfully!"));
        }
    }
}
