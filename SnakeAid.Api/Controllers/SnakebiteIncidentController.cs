using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Requests.PayOS;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.PayOs;
using SnakeAid.Core.Responses.PayOS;
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
        private readonly ISnakebiteIncidentPaymentService _incidentPaymentService;

        public SnakebiteIncidentController(
            ILogger<SnakebiteIncidentController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ISnakebiteIncidentService incidentService,
            ISnakebiteIncidentPaymentService incidentPaymentService)
            : base(logger, httpContextAccessor, mapper)
        {
            _incidentService = incidentService;
            _incidentPaymentService = incidentPaymentService;
        }

        /// <summary>
        /// Create snakebite incident SOS report — Operator will be notified and handle dispatch
        /// </summary>
        [HttpPost("sos")]
        [SwaggerOperation(Summary = "Create Snakebite Incident", Description = "Report Snakebite Incident Emergency Rescue. Incident enters Pending state and is broadcast to on-duty Operators.")]
        [SwaggerResponse(200, "Create successful", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(400, "Member profile not found")]
        [SwaggerResponse(422, "Validation error")]
        [Authorize]
        [ValidateModel]
        public async Task<IActionResult> CreateSnakebiteIncident([FromBody] CreateIncidentRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _incidentService.CreateIncidentAsync(request, userId);
            var response = ApiResponseBuilder.BuildSuccessResponse(result,
                "Snakebite incident created. An operator will contact you shortly.");
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Create a PayOS payment link for a snakebite incident
        /// </summary>
        [HttpPost("{incidentId}/payment/payos")]
        [Authorize]
        [SwaggerOperation(Summary = "Create Snakebite Incident PayOS Payment", Description = "Create a PayOS payment link for snakebite incident processing.")]
        [SwaggerResponse(200, "Payment link created successfully", typeof(ApiResponse<SnakebiteIncidentPaymentResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(404, "Incident not found")]
        public async Task<IActionResult> CreateSnakebiteIncidentPaymentLink(Guid incidentId, [FromBody] CreateSnakebiteIncidentPaymentRequest request)
        {
            request.SnakebiteIncidentId = incidentId;
            var currentUserId = GetCurrentUserId();

            var result = await _incidentPaymentService.CreateSnakebiteIncidentPaymentLinkAsync(request, currentUserId, HttpContext.RequestAborted);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Payment link created successfully."));
        }

        /// <summary>
        /// Pay for snakebite incident using in-app wallet
        /// </summary>
        [HttpPost("{incidentId}/payment/wallet")]
        [Authorize]
        [SwaggerOperation(Summary = "Pay Snakebite Incident via Wallet", Description = "Pay snakebite incident fee using the user's wallet balance.")]
        [SwaggerResponse(200, "Payment completed successfully", typeof(ApiResponse<SnakebiteIncidentPaymentResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(404, "Incident not found")]
        public async Task<IActionResult> PaySnakebiteIncidentWithWallet(Guid incidentId, [FromBody] CreateSnakebiteIncidentPaymentRequest request)
        {
            request.SnakebiteIncidentId = incidentId;
            var currentUserId = GetCurrentUserId();

            var result = await _incidentPaymentService.CreateSnakebiteIncidentWalletPaymentAsync(request, currentUserId, HttpContext.RequestAborted);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Wallet payment completed successfully."));
        }

        /// <summary>
        /// Cancel a PayOS payment link for a snakebite incident
        /// </summary>
        [HttpDelete("{incidentId}/payment/payos/{orderCode}")]
        [Authorize]
        [SwaggerOperation(Summary = "Cancel Snakebite Incident PayOS Payment Link", Description = "Cancel an existing PayOS payment link for a snakebite incident.")]
        [SwaggerResponse(200, "Payment link cancelled successfully", typeof(ApiResponse<CancelPaymentLinkResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(404, "Incident not found")]
        public async Task<IActionResult> CancelSnakebiteIncidentPaymentLink(Guid incidentId, long orderCode, [FromBody] CancelPaymentLinkRequest request)
        {
            var result = await _incidentPaymentService.CancelSnakebiteIncidentPaymentLinkAsync(orderCode, request, HttpContext.RequestAborted);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Payment link cancelled successfully."));
        }

        /// <summary>
        /// Refund a snakebite incident payment (Admin/Operator only)
        /// </summary>
        [HttpPost("{incidentId}/payment/refund")]
        [Authorize(Roles = "Operator,Admin")]
        [SwaggerOperation(Summary = "Refund Snakebite Incident Payment", Description = "Refund a snakebite incident payment. Only accessible by Operators and Admins.")]
        [SwaggerResponse(200, "Refund successful", typeof(ApiResponse<RefundTransactionResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(404, "Payment not found")]
        [SwaggerResponse(409, "Insufficient balance")]
        public async Task<IActionResult> RefundSnakebiteIncidentPayment(Guid incidentId, [FromBody] RefundTransactionRequest request)
        {
            request.ReferenceId = incidentId;
            var result = await _incidentPaymentService.RefundSnakebiteIncidentTransactionAsync(request, HttpContext.RequestAborted);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Refund completed successfully."));
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
        [Authorize]
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
        [Authorize]
        [ValidateModel]
        public async Task<IActionResult> CancelIncident(Guid incidentId, [FromBody] CancelIncidentRequest request)
        {
            var result = await _incidentService.CancelIncidentAsync(incidentId, request);
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
        [SwaggerOperation(Summary = "Identify Snake by Filter", Description = "Set the identified snake species for an incident based on user's selected snake species by location")]
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
        [Authorize]
        public async Task<IActionResult> GetUserIncidents([FromQuery] SnakebiteIncidentStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var result = await _incidentService.GetUserIncidentsAsync(userId, status, page, pageSize);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "User incidents retrieved"));
        }

        [HttpGet("active")]
        [Authorize(Roles = "Operator,Admin")]
        [SwaggerOperation(Summary = "Get Active Incidents", Description = "Retrieve active incidents (not finished/cancelled) for operator dashboard and map.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedData<OperatorIncidentSummaryResponse>>))]
        public async Task<IActionResult> GetActiveIncidents(
            [FromQuery] string? status = null,
            [FromQuery] DateTimeOffset? since = null,
            [FromQuery] DateTimeOffset? until = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var statuses = ParseStatuses(status);
            var result = await _incidentService.GetActiveIncidentsAsync(statuses, since, until, page, pageSize);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Get paged incident list for admin dashboard.
        /// </summary>
        [HttpGet("admin/list")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get Admin Incident List", Description = "Retrieve paged incident summaries for admin with optional status/time filters.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedData<OperatorIncidentSummaryResponse>>))]
        public async Task<IActionResult> GetAdminIncidentList(
            [FromQuery] string? status = null,
            [FromQuery] DateTimeOffset? since = null,
            [FromQuery] DateTimeOffset? until = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var statuses = ParseStatuses(status);
            var result = await _incidentService.GetAdminIncidentsAsync(statuses, since, until, page, pageSize);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Admin incident list retrieved."));
        }

        /// <summary>
        /// Get detailed incident information for admin, including full mission and dispatch request histories.
        /// </summary>
        [HttpGet("admin/{incidentId}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get Admin Incident Detail", Description = "Retrieve full admin detail for an incident, including all rescue missions, dispatch requests, and media.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<AdminDetailSnakebiteIncidentResponse>))]
        [SwaggerResponse(404, "Incident not found")]
        public async Task<IActionResult> GetAdminIncidentDetail(Guid incidentId)
        {
            var result = await _incidentService.GetAdminDetailIncidentAsync(incidentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Admin incident detail retrieved."));
        }

        private static IEnumerable<SnakebiteIncidentStatus>? ParseStatuses(string? csvStatuses)
        {
            if (string.IsNullOrWhiteSpace(csvStatuses))
                return null;

            var values = new List<SnakebiteIncidentStatus>();
            foreach (var rawValue in csvStatuses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse(rawValue, true, out SnakebiteIncidentStatus parsed))
                {
                    values.Add(parsed);
                }
            }

            return values.Count > 0 ? values : null;
        }

        /// <summary>
        /// Operator confirms incident is real after contact (and claims it if not already claimed)
        /// </summary>
        [HttpPost("{incidentId}/confirm")]
        [SwaggerOperation(Summary = "Confirm Incident", Description = "Confirm incident after operator contact. Returns 409 on concurrency conflict.")]
        [SwaggerResponse(200, "Incident confirmed", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(409, "Incident updated by another operator")]
        [Authorize(Roles = "Operator")]
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
        [Authorize(Roles = "Operator")]
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
        [Authorize(Roles = "Operator")]
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
        [Authorize(Roles = "Operator")]
        public async Task<IActionResult> DispatchIncident(Guid incidentId, [FromBody] DispatchIncidentRequest request)
        {
            var operatorId = GetCurrentUserId();
            var result = await _incidentService.DispatchIncidentAsync(incidentId, request.RescuerId, operatorId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Incident dispatched."));
        }

        /// <summary>
        /// Operator cancels a dispatch request
        /// </summary>
        [HttpPost("dispatch-requests/{requestId}/cancel")]
        [SwaggerOperation(Summary = "Cancel Dispatch Request", Description = "Cancel a pending dispatch request sent to a rescuer.")]
        [SwaggerResponse(200, "Dispatch request cancelled", typeof(ApiResponse<RejectRescueResponse>))]
        [SwaggerResponse(404, "Dispatch request not found")]
        [SwaggerResponse(409, "Dispatch request is not pending or updated by another process")]
        [Authorize(Roles = "Operator")]
        public async Task<IActionResult> CancelDispatchRequest(Guid requestId)
        {
            var operatorId = GetCurrentUserId();
            var result = await _incidentService.CancelDispatchRequestAsync(requestId, operatorId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Dispatch request cancelled."));
        }

        /// <summary>
        /// Get dispatch requests for a given incident.
        /// </summary>
        /// <remarks>
        /// Frontend can call this endpoint to get the list of dispatch requests associated with the specified incident Id.
        /// </remarks>
        [HttpGet("{incidentId}/dispatch-requests")]
        [SwaggerOperation(Summary = "Get Dispatch Requests for Incident", Description = "Retrieve all dispatch requests for a specific incident (by incidentId).")]
        [SwaggerResponse(200, "Dispatch requests retrieved", typeof(ApiResponse<IEnumerable<DispatchRequestResponse>>))]
        [SwaggerResponse(404, "Incident not found")]
        [Authorize(Roles = "Operator, Admin")]
        public async Task<IActionResult> GetDispatchRequests(Guid incidentId)
        {
            var result = await _incidentService.GetDispatchRequestsAsync(incidentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Dispatch requests retrieved."));
        }
    }
}
