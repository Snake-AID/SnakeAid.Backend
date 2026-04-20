using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SnakeCatchingMission;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;

namespace SnakeAid.Api.Controllers
{
    [Route("api/snakecatching/missions")]
    [ApiController]
    [Authorize]
    public class SnakeCatchingMissionController : BaseController<SnakeCatchingMissionController>
    {
        private readonly ISnakeCatchingMissionService _missionService;

        public SnakeCatchingMissionController(
            ILogger<SnakeCatchingMissionController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ISnakeCatchingMissionService missionService)
            : base(logger, httpContextAccessor, mapper)
        {
            _missionService = missionService;
        }

        /// <summary>
        /// Start snake catching mission - transition to EnRoute
        /// </summary>
        [HttpPatch("{missionId}/start")]
        [SwaggerOperation(
            Summary = "Start Mission",
            Description = "Start the snake catching mission (Preparing → EnRoute). Rescuer begins heading to the catching location.")]
        [SwaggerResponse(200, "Mission started successfully", typeof(ApiResponse<SnakeCatchingMissionDetailResponse>))]
        [SwaggerResponse(400, "Invalid status transition")]
        [SwaggerResponse(403, "Not authorized")]
        [SwaggerResponse(404, "Mission not found")]
        public async Task<IActionResult> StartMission(
            Guid missionId,
            [FromBody] UpdateMissionStatusRequest request)
        {
            var rescuerId = GetCurrentUserId();
            var result = await _missionService.StartMissionAsync(rescuerId, missionId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Mission started! En route to location."));
        }

        /// <summary>
        /// Mark mission as arrived - transition to Arrived
        /// </summary>
        [HttpPatch("{missionId}/arrived")]
        [SwaggerOperation(
            Summary = "Mark as Arrived",
            Description = "Mark the snake catching mission as arrived (EnRoute → Arrived). Rescuer has reached the location.")]
        [SwaggerResponse(200, "Mission marked as arrived successfully", typeof(ApiResponse<SnakeCatchingMissionDetailResponse>))]
        [SwaggerResponse(400, "Invalid status transition")]
        [SwaggerResponse(403, "Not authorized")]
        [SwaggerResponse(404, "Mission not found")]
        public async Task<IActionResult> MarkAsArrived(
            Guid missionId,
            [FromBody] UpdateMissionStatusRequest request)
        {
            var rescuerId = GetCurrentUserId();
            var result = await _missionService.MarkAsArrivedAsync(rescuerId, missionId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Mission marked as arrived!"));
        }

        /// <summary>
        /// Complete mission - transition to MissionCompleted
        /// Requires evidence media in the snake catching request
        /// Automatically updates the request status to Finished
        /// </summary>
        [HttpPatch("{missionId}/complete")]
        [SwaggerOperation(
            Summary = "Complete Mission",
            Description = @"Complete the snake catching mission (Arrived → MissionCompleted).

Requirements:
- Mission must be in Arrived status
- SnakeCatchingRequest must have at least one evidence media uploaded

When completed successfully:
- Mission status is updated to MissionCompleted
- CompletedAt timestamp is set
- SnakeCatchingRequest status is automatically updated to Completed
- Response includes list of catching mission details (snake species and quantities) if available")]
        [SwaggerResponse(200, "Mission completed successfully", typeof(ApiResponse<SnakeCatchingMissionDetailResponse>))]
        [SwaggerResponse(400, "Invalid status transition or missing evidence media")]
        [SwaggerResponse(403, "Not authorized")]
        [SwaggerResponse(404, "Mission not found")]
        public async Task<IActionResult> CompleteMission(
            Guid missionId,
            [FromBody] UpdateMissionStatusRequest request)
        {
            var rescuerId = GetCurrentUserId();
            var result = await _missionService.CompleteMissionAsync(rescuerId, missionId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                result,
                "Mission completed successfully! Request marked as completed."));
        }

        /// <summary>
        /// Mark mission as uncompleted - transition to MissionUncompleted
        /// Requires reason and at least one evidence media in snake catching request
        /// Automatically updates the request status to Completed
        /// </summary>
        [HttpPatch("{missionId}/uncomplete")]
        [SwaggerOperation(
            Summary = "Uncomplete Mission",
            Description = @"Mark the snake catching mission as uncompleted (Arrived → MissionUncompleted).

Requirements:
- Mission must be in Arrived status
- Reason is required
- SnakeCatchingRequest must have at least one evidence media uploaded

When uncompleted successfully:
- Mission status is updated to MissionUncompleted
- CancellationReason is set to the provided reason
- CompletedAt timestamp is set
- SnakeCatchingRequest status is automatically updated to Completed")]
        [SwaggerResponse(200, "Mission marked as uncompleted successfully", typeof(ApiResponse<SnakeCatchingMissionDetailResponse>))]
        [SwaggerResponse(400, "Invalid status transition, missing reason, or missing evidence media")]
        [SwaggerResponse(403, "Not authorized")]
        [SwaggerResponse(404, "Mission not found")]
        public async Task<IActionResult> UncompleteMission(
            Guid missionId,
            [FromBody] UncompleteSnakeCatchingMissionRequest request)
        {
            var rescuerId = GetCurrentUserId();
            var result = await _missionService.UncompleteMissionAsync(rescuerId, missionId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                result,
                "Mission marked as uncompleted. Request marked as completed."));
        }

        /// <summary>
        /// Abort mission - transition to MissionAborted
        /// Can only abort when mission status is Preparing or EnRoute
        /// </summary>
        [HttpPatch("{missionId}/abort")]
        [SwaggerOperation(
            Summary = "Abort Mission",
            Description = @"Abort the snake catching mission (Preparing/EnRoute → MissionAborted).

Requirements:
- Mission must be in Preparing or EnRoute status
- Reason is required

When aborted successfully:
- Mission status is updated to MissionAborted
- CancellationReason is set to the provided reason
- SnakeCatchingRequest is reset to Pending status
- AssignedRescuer is cleared so other rescuers can accept the request
- If any paid transactions exist (CatchingPayment or CatchingDeposit), automatic refund is processed to user's wallet")]
        [SwaggerResponse(200, "Mission aborted successfully", typeof(ApiResponse<SnakeCatchingMissionDetailResponse>))]
        [SwaggerResponse(400, "Invalid status transition - can only abort from Preparing or EnRoute")]
        [SwaggerResponse(403, "Not authorized")]
        [SwaggerResponse(404, "Mission not found")]
        public async Task<IActionResult> AbortMission(
            Guid missionId,
            [FromBody] AbortSnakeCatchingMissionRequest request)
        {
            var rescuerId = GetCurrentUserId();
            var result = await _missionService.AbortMissionAsync(rescuerId, missionId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                result,
                "Mission aborted successfully."));
        }
    }
}
