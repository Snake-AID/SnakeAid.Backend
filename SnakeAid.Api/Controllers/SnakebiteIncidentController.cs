using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Core.Validators;
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
        /// Create snakebite incident SOS report — Operator will be notified and handle dispatch
        /// </summary>
        [HttpPost("sos")]
        [SwaggerOperation(Summary = "Create Snakebite Incident", Description = "Report Snakebite Incident Emergency Rescue. Incident enters Pending state and is broadcast to on-duty Operators.")]
        [SwaggerResponse(200, "Create successful", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(400, "Member profile not found")]
        [SwaggerResponse(422, "Validation error")]
        public async Task<IActionResult> CreateSnakebiteIncident([FromBody] CreateIncidentRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _incidentService.CreateIncidentAsync(request, userId);
            var response = ApiResponseBuilder.BuildSuccessResponse(result,
                "Snakebite incident created. An operator will contact you shortly.");
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Get detailed information about a specific snakebite incident
        /// </summary>
        [HttpGet("{incidentId}")]
        [SwaggerOperation(Summary = "Get Incident Detail", Description = "Retrieve detailed information about a snakebite incident including user, rescuer, and media")]
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
        [ValidateModel]
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
        [SwaggerOperation(Summary = "Cancel Incident", Description = "Cancel a snakebite incident if it is in Pending status")]
        [SwaggerResponse(200, "Incident cancelled successfully", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(404, "Incident not found")]
        [SwaggerResponse(422, "Validation error")]
        public async Task<IActionResult> CancelIncident(Guid incidentId)
        {
            var result = await _incidentService.CancelIncidentAsync(incidentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Incident cancelled successfully!"));
        }

        /// <summary>
        /// Identify snake species for incident using AI recognition result
        /// </summary>
        [HttpPost("{incidentId}/identify/ai")]
        [SwaggerOperation(Summary = "Identify Snake by AI", Description = "Set the identified snake species for an incident based on AI recognition result")]
        [SwaggerResponse(200, "Snake identified successfully", typeof(ApiResponse<IdentifySnakeResponse>))]
        [SwaggerResponse(404, "Incident or recognition result not found")]
        [SwaggerResponse(400, "Invalid recognition result")]
        public async Task<IActionResult> IdentifySnakeByAI(Guid incidentId, [FromBody] IdentifyByAIRequest request)
        {
            var result = await _incidentService.IdentifySnakeByAIAsync(incidentId, request.RecognitionResultId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Snake identified successfully via AI detection!"));
        }

        /// <summary>
        /// Identify snake species for incident using filter questions
        /// </summary>
        [HttpPost("{incidentId}/identify/filter")]
        [SwaggerOperation(Summary = "Identify Snake by Filter", Description = "Set the identified snake species for an incident based on user's answers to filter questions")]
        [SwaggerResponse(200, "Snake identified successfully", typeof(ApiResponse<IdentifySnakeResponse>))]
        [SwaggerResponse(404, "Incident not found")]
        [SwaggerResponse(400, "Invalid filter answers or no matching snake found")]
        public async Task<IActionResult> IdentifySnakeByFilter(Guid incidentId, [FromBody] IdentifyByFilterRequest request)
        {
            var result = await _incidentService.IdentifySnakeByFilterAsync(incidentId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Snake identified successfully via filter questions!"));
        }

        /// <summary>
        /// DEBUG: Get raw media count for incident, test if media are being saved correctly in database
        /// </summary>
        [HttpGet("{incidentId}/debug/media-count")]
        [SwaggerOperation(Summary = "Debug Media Count", Description = "Check how many media items exist for this incident in database")]
        public async Task<IActionResult> DebugMediaCount(Guid incidentId)
        {
            var result = await _incidentService.GetMediaDebugInfoAsync(incidentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Debug info retrieved"));
        }

        [HttpGet("user/{userId}")]
        [SwaggerOperation(Summary = "Get User Incidents", Description = "Retrieve all incidents associated with a specific user (status filter is optional)")]
        public async Task<IActionResult> GetUserIncidents([FromQuery] SnakebiteIncidentStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _incidentService.GetUserIncidentsAsync(userId, status, page, pageSize);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "User incidents retrieved"));
        }

        /// <summary>
        /// Operator claims an incident for handling
        /// </summary>
        [HttpPost("{incidentId}/claim")]
        [SwaggerOperation(Summary = "Claim Incident", Description = "Claim a pending incident. Returns 409 if another operator claimed first.")]
        [SwaggerResponse(200, "Incident claimed successfully", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(409, "Incident already claimed")]
        public async Task<IActionResult> ClaimIncident(Guid incidentId)
        {
            var operatorId = GetCurrentUserId();
            var result = await _incidentService.ClaimIncidentAsync(incidentId, operatorId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Incident claimed successfully."));
        }

        /// <summary>
        /// Operator confirms incident is real after contact
        /// </summary>
        [HttpPost("{incidentId}/confirm")]
        [SwaggerOperation(Summary = "Confirm Incident", Description = "Confirm incident after operator contact. Returns 409 on concurrency conflict.")]
        [SwaggerResponse(200, "Incident confirmed", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(409, "Incident updated by another operator")]
        public async Task<IActionResult> ConfirmIncident(Guid incidentId)
        {
            var operatorId = GetCurrentUserId();
            var result = await _incidentService.ConfirmIncidentAsync(incidentId, operatorId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Incident confirmed."));
        }

        /// <summary>
        /// Operator marks incident as a false alarm
        /// </summary>
        [HttpPost("{incidentId}/false-alarm")]
        [SwaggerOperation(Summary = "Mark Incident as False Alarm", Description = "Mark an incident as a false alarm after operator call.")]
        [SwaggerResponse(200, "Incident marked as false alarm", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(404, "Incident not found")]
        [SwaggerResponse(409, "Incident updated by another operator")]
        public async Task<IActionResult> MarkFalseAlarm(Guid incidentId, [FromBody] MarkFalseAlarmRequest request)
        {
            var operatorId = GetCurrentUserId();
            var result = await _incidentService.MarkIncidentFalseAlarmAsync(incidentId, operatorId, request?.Reason);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Incident marked as false alarm."));
        }

        /// <summary>
        /// Operator reports no answer from member
        /// </summary>
        [HttpPost("{incidentId}/no-answer")]
        [SwaggerOperation(Summary = "Report No Answer", Description = "Report that the member did not answer the operator's call. Operator can choose to continue calling or release the case.")]
        [SwaggerResponse(200, "No answer recorded", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(404, "Incident not found")]
        [SwaggerResponse(409, "Incident updated by another operator")]
        public async Task<IActionResult> ReportNoAnswer(Guid incidentId, [FromBody] ReportNoAnswerRequest request)
        {
            var operatorId = GetCurrentUserId();
            var result = await _incidentService.ReportIncidentNoAnswerAsync(incidentId, operatorId, request?.ContinueCalling ?? true, request?.Note);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "No answer recorded."));
        }

        /// <summary>
        /// Operator dispatches an incident to a rescuer
        /// </summary>
        [HttpPost("{incidentId}/dispatch")]
        [SwaggerOperation(Summary = "Dispatch Incident", Description = "Dispatch incident to a rescuer. Returns 409 on concurrency conflict.")]
        [SwaggerResponse(200, "Incident dispatched", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(409, "Incident updated by another operator")]
        public async Task<IActionResult> DispatchIncident(Guid incidentId, [FromBody] DispatchIncidentRequest request)
        {
            var operatorId = GetCurrentUserId();
            var result = await _incidentService.DispatchIncidentAsync(incidentId, request.RescuerId, operatorId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Incident dispatched."));
        }
    }
}
