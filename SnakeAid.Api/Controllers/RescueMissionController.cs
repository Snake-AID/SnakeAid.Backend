using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.RescueMission;
using SnakeAid.Core.Responses.RescueMission;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Api.Controllers
{
    [Route("api/rescue-missions")]
    [ApiController]
    [Authorize]
    public class RescueMissionController : BaseController<RescueMissionController>
    {
        private readonly ISnakeRescueMissionService _missionService;

        public RescueMissionController(
            ILogger<RescueMissionController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ISnakeRescueMissionService missionService)
            : base(logger, httpContextAccessor, mapper)
        {
            _missionService = missionService;
        }

        /// <summary>
        /// Get rescue mission details
        /// Rescuer uses this before starting movement to see full incident info with images
        /// </summary>
        /// <param name="missionId">Mission ID</param>
        /// <param name="rescuerLat">Rescuer's current latitude (for distance calculation)</param>
        /// <param name="rescuerLng">Rescuer's current longitude (for distance calculation)</param>
        [HttpGet("{missionId}")]
        [SwaggerOperation(
            Summary = "Get Mission Details",
            Description = @"Retrieve detailed mission information for rescuer including:
            - Patient info (name, avatar, emergency contacts)
            - Incident location and symptoms
            - Snake images with AI recognition results
            - Severity level
            - Distance from rescuer (if location provided)
            Used before rescuer starts moving to location.")]
        [SwaggerResponse(200, "Mission details retrieved successfully", typeof(ApiResponse<DetailRescueMissionResponse>))]
        [SwaggerResponse(404, "Mission not found")]
        [Authorize(Roles = "Rescuer, Admin")]
        public async Task<IActionResult> GetMissionDetails(
            Guid missionId,
            [FromQuery] double? rescuerLat = null,
            [FromQuery] double? rescuerLng = null)
        {
            var result = await _missionService.GetMissionDetailAsync(missionId, rescuerLat, rescuerLng);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Mission details retrieved successfully!"));
        }

        /// <summary>
        /// Get paged rescue mission list for admin dashboard.
        /// </summary>
        [HttpGet("admin/list")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Get Admin Rescue Mission List",
            Description = "Retrieve paged rescue mission summaries for admin with optional status/time filters.")]
        [SwaggerResponse(200, "Rescue missions retrieved successfully", typeof(ApiResponse<PagedData<AdminRescueMissionSummaryResponse>>))]
        public async Task<IActionResult> GetAdminMissionList(
            [FromQuery] string? status = null,
            [FromQuery] DateTimeOffset? since = null,
            [FromQuery] DateTimeOffset? until = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var statuses = ParseMissionStatuses(status);
            var result = await _missionService.GetAdminMissionListAsync(statuses, since, until, page, pageSize);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Admin rescue mission list retrieved."));
        }

        /// <summary>
        /// Get rescuer mission list for current authenticated rescuer
        /// </summary>
        [HttpGet("rescuer/list")]
        [Authorize(Roles = "Rescuer")]
        [SwaggerOperation(
            Summary = "Get My Rescue Missions",
            Description = "Retrieve the authenticated rescuer's rescue missions filtered by optional status. Omit status to return all missions assigned to the current rescuer.")]
        [SwaggerResponse(200, "Rescue missions retrieved successfully", typeof(ApiResponse<List<ListRescueMissionResponse>>))]
        public async Task<IActionResult> GetMyRescueMissionList([FromQuery] RescueMissionStatus? status = null)
        {
            var rescuerId = GetCurrentUserId();
            var result = await _missionService.GetRescuerMissionListAsync(rescuerId, status);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, $"Retrieved {result.Count} rescue mission(s) successfully."));
        }

        private static IEnumerable<RescueMissionStatus>? ParseMissionStatuses(string? csvStatuses)
        {
            if (string.IsNullOrWhiteSpace(csvStatuses))
            {
                return null;
            }

            var values = new List<RescueMissionStatus>();
            foreach (var rawValue in csvStatuses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse(rawValue, true, out RescueMissionStatus parsed))
                {
                    values.Add(parsed);
                }
            }

            return values.Count > 0 ? values : null;
        }


        /// <summary>
        /// Start mission - transition to EnRoute
        /// </summary>
        [HttpPatch("{missionId}/start")]
        [SwaggerOperation(
            Summary = "Start Mission",
            Description = "Start the rescue mission (Preparing → EnRoute). Rescuer begins heading to the incident location.")]
        [SwaggerResponse(200, "Mission started successfully")]
        [SwaggerResponse(400, "Invalid status transition")]
        [SwaggerResponse(404, "Mission not found")]
        [Authorize(Roles = "Rescuer")]
        public async Task<IActionResult> StartMission(Guid missionId)
        {
            await _missionService.UpdateMissionStatusAsync(missionId, RescueMissionStatus.EnRoute);
            return Ok(ApiResponseBuilder.BuildSuccessResponse<object>(null, "Mission started! En route to location."));
        }

        /// <summary>
        /// Mark arrival at location - transition to RescuerArrived
        /// </summary>
        [HttpPatch("{missionId}/arrive")]
        [SwaggerOperation(
            Summary = "Arrive at Location",
            Description = "Mark rescuer's arrival at the incident location (EnRoute → RescuerArrived).")]
        [SwaggerResponse(200, "Arrival marked successfully")]
        [SwaggerResponse(400, "Invalid status transition")]
        [SwaggerResponse(404, "Mission not found")]
        [Authorize(Roles = "Rescuer")]
        public async Task<IActionResult> ArriveAtLocation(Guid missionId)
        {
            await _missionService.UpdateMissionStatusAsync(missionId, RescueMissionStatus.RescuerArrived);
            return Ok(ApiResponseBuilder.BuildSuccessResponse<object>(null, "Arrival marked successfully!"));
        }

        /// <summary>
        /// Complete mission with evidence photos
        /// Requires at least one evidence photo to be uploaded beforehand via /api/media/report
        /// </summary>
        /// <param name="missionId">Mission ID</param>
        /// <param name="request">Evidence media IDs and optional notes</param>
        [HttpPatch("{missionId}/complete")]
        [SwaggerOperation(
            Summary = "Complete Mission with Evidence",
            Description = @"Complete the rescue mission (RescuerArrived → MissionCompleted) with evidence photos.
            
            **Workflow:**
            1. Upload evidence photos via POST /api/media/report?type=RescueMission&purpose=Evidence with referenceId=missionId
            2. Collect the returned ReportMedia IDs
            3. Call this endpoint with the list of media IDs
            
            **Validations:**
            - At least one evidence photo required
            - All media must belong to this mission (ReferenceId=missionId, ReferenceType=RescueMission)
            - All media must have Purpose=Evidence
            - Media with Purpose=Evidence will NOT trigger AI processing
            
            Automatically updates incident status to Finished.")]
        [SwaggerResponse(200, "Mission completed successfully", typeof(ApiResponse<object>))]
        [SwaggerResponse(400, "Invalid request or evidence validation failed", typeof(ApiResponse<object>))]
        [SwaggerResponse(404, "Mission not found")]
        [Authorize(Roles = "Rescuer")]
        [ValidateModel]
        public async Task<IActionResult> CompleteMission(
            Guid missionId,
            [FromBody] CompleteMissionRequest request)
        {
            await _missionService.CompleteMissionAsync(missionId, request.EvidenceMediaIds, request.CompletionNotes);
            return Ok(ApiResponseBuilder.BuildSuccessResponse<object>(null, "Mission completed successfully with evidence photos! Incident marked as finished."));
        }

        /// <summary>
        /// Abort mission - rescuer cannot complete
        /// Creates a new session with increased radius for finding another rescuer
        /// </summary>
        [HttpPatch("{missionId}/abort")]
        [SwaggerOperation(
            Summary = "Abort Mission (Rescuer)",
            Description = "Rescuer aborts the mission with a reason (Preparing/EnRoute → MissionAborted). Incident is reset to Pending and a new rescue session is created with increased radius.")]
        [SwaggerResponse(200, "Mission aborted, new session created", typeof(ApiResponse<object>))]
        [SwaggerResponse(400, "Invalid status transition or missing reason", typeof(ApiResponse<object>))]
        [SwaggerResponse(404, "Mission not found")]
        [Authorize(Roles = "Rescuer")]
        [ValidateModel]
        public async Task<IActionResult> AbortMission(
            Guid missionId,
            [FromBody] AbortMissionRequest request)
        {
            await _missionService.RescuerAbortMissionAsync(missionId, request.CancellationReason);
            return Ok(ApiResponseBuilder.BuildSuccessResponse<object>(null, "Mission aborted. New rescue session created with increased radius."));
        }

        /// <summary>
        /// Cancel mission - user cancels before rescuer starts
        /// No new session is created
        /// </summary>
        [Obsolete("Use /api/incidents/{incidentId}/cancel instead")]
        [HttpPatch("{missionId}/cancel")]
        [SwaggerOperation(
            Summary = "Cancel Mission (User)",
            Description = "User cancels the mission before rescuer goes en route (Preparing → Cancelled). Incident is set to Cancelled. No new session is created.")]
        [SwaggerResponse(200, "Mission cancelled", typeof(ApiResponse<object>))]
        [SwaggerResponse(400, "Invalid status transition - can only cancel during Preparing phase", typeof(ApiResponse<object>))]
        [SwaggerResponse(404, "Mission not found")]
        [Authorize(Roles = "User")]
        [ValidateModel]
        public async Task<IActionResult> CancelMission(
            Guid missionId,
            [FromBody] CancelMissionRequest request)
        {
            await _missionService.UserCancelMissionAsync(missionId, request.CancellationReason ?? "No reason provided");
            return Ok(ApiResponseBuilder.BuildSuccessResponse<object>(null, "Mission cancelled by user."));
        }

        [HttpPatch("{missionId}/hospital-transfer")]
        [SwaggerOperation(
            Summary = "Transfer Mission to Hospital (User)",
            Description = "Rescuer reports hospital transfer needed for the patient with hospital info")]
        [SwaggerResponse(200, "Mission transferred to hospital", typeof(ApiResponse<HospitalTransferPricingResponse>))]
        [SwaggerResponse(400, "Invalid status transition - can only transfer during Preparing phase", typeof(ApiResponse<object>))]
        [SwaggerResponse(404, "Mission not found")]
        [Authorize(Roles = "Rescuer")]
        [ValidateModel]
        public async Task<IActionResult> TransferToHospital(
            Guid missionId,
            [FromBody] ReportHospitalTransferRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _missionService.ReportHospitalTransferAsync(missionId, userId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse<HospitalTransferPricingResponse>(result, "Mission transferred to hospital."));
        }

        [HttpPatch("{missionId}/no-need-hospital-transfer")]
        [SwaggerOperation(
            Summary = "Report No Need for Hospital Transfer (User)",
            Description = "Rescuer reports that hospital transfer is not needed for the patient")]
        [SwaggerResponse(200, "No need for hospital transfer reported", typeof(ApiResponse<bool>))]
        [SwaggerResponse(400, "Invalid status transition - can only report no need for hospital transfer during Preparing phase", typeof(ApiResponse<object>))]
        [SwaggerResponse(404, "Mission not found")]
        [Authorize(Roles = "Rescuer")]
        [ValidateModel]
        public async Task<IActionResult> ReportNoNeedHospitalTransfer(
            Guid missionId,
            [FromBody] ReportNoNeedHospitalTransferRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _missionService.ReportNoNeedHospitalTransferAsync(missionId, userId, request.Notes);
            return Ok(ApiResponseBuilder.BuildSuccessResponse<bool>(result, "No need for hospital transfer reported."));
        }
    }
}
