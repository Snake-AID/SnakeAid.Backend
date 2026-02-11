using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.RescueMission;
using SnakeAid.Core.Responses.RescueMission;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;

namespace SnakeAid.Api.Controllers
{
    [Route("api/rescue-missions")]
    [ApiController]
    [Authorize]
    public class RescueMissionController : BaseController<RescueMissionController>
    {
        private readonly IRescueMissionService _missionService;

        public RescueMissionController(
            ILogger<RescueMissionController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IRescueMissionService missionService)
            : base(logger, httpContextAccessor, mapper)
        {
            _missionService = missionService;
        }

        // Note: GetMissionDetailsAsync is not defined in IRescueMissionService interface
        // Commenting out until it's added to the interface
        // /// <summary>
        // /// Get rescue mission details
        // /// </summary>
        // [HttpGet("{missionId}")]
        // [SwaggerOperation(Summary = "Get Mission Details", Description = "Retrieve detailed information about a rescue mission")]
        // [SwaggerResponse(200, "Mission details retrieved successfully", typeof(ApiResponse<RescueMissionStatusResponse>))]
        // [SwaggerResponse(404, "Mission not found")]
        // public async Task<IActionResult> GetMissionDetails(Guid missionId)
        // {
        //     var result = await _missionService.GetMissionDetailsAsync(missionId);
        //     return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Mission details retrieved successfully!"));
        // }

        /// <summary>
        /// Update rescue mission status
        /// When status is set to MissionCompleted, the corresponding incident is automatically updated to Finished
        /// </summary>
        [HttpPatch("{missionId}/status")]
        [SwaggerOperation(
            Summary = "Update Mission Status",
            Description = @"Update the status of a rescue mission. 

 Valid status transitions:
 - Preparing → EnRoute, Cancelled
 - EnRoute → RescuerArrived, MissionAborted
 - RescuerArrived → MissionCompleted, MissionUncompleted, MissionAborted

 When transitioning to MissionCompleted:
 - The corresponding SnakebiteIncident status is automatically updated to Finished")]
        [SwaggerResponse(200, "Mission status updated successfully")]
        [SwaggerResponse(400, "Invalid status transition")]
        [SwaggerResponse(404, "Mission not found")]
        public async Task<IActionResult> UpdateMissionStatus(
            Guid missionId,
            [FromBody] UpdateRescueMissionStatusRequest request)
        {
            await _missionService.UpdateMissionStatusAsync(missionId, request.Status);

            var message = request.Status == RescueMissionStatus.MissionCompleted
                ? "Mission completed successfully! Incident marked as finished."
                : $"Mission status updated to {request.Status} successfully!";

            return Ok(ApiResponseBuilder.BuildSuccessResponse<object>(null, message));
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
        public async Task<IActionResult> ArriveAtLocation(Guid missionId)
        {
            await _missionService.UpdateMissionStatusAsync(missionId, RescueMissionStatus.RescuerArrived);
            return Ok(ApiResponseBuilder.BuildSuccessResponse<object>(null, "Arrival marked successfully!"));
        }

        /// <summary>
        /// Complete mission - transition to MissionCompleted
        /// Updates incident to Finished
        /// </summary>
        [HttpPatch("{missionId}/complete")]
        [SwaggerOperation(
            Summary = "Complete Mission",
            Description = "Complete the rescue mission (RescuerArrived → MissionCompleted). Automatically updates incident status to Finished.")]
        [SwaggerResponse(200, "Mission completed successfully")]
        [SwaggerResponse(400, "Invalid status transition")]
        [SwaggerResponse(404, "Mission not found")]
        public async Task<IActionResult> CompleteMission(Guid missionId)
        {
            await _missionService.UpdateMissionStatusAsync(missionId, RescueMissionStatus.MissionCompleted);
            return Ok(ApiResponseBuilder.BuildSuccessResponse<object>(null, "Mission completed successfully! Incident marked as finished."));
        }

        /// <summary>
        /// Abort mission - rescuer cannot complete
        /// Creates a new session with increased radius for finding another rescuer
        /// </summary>
        [HttpPatch("{missionId}/abort")]
        [SwaggerOperation(
            Summary = "Abort Mission (Rescuer)",
            Description = "Rescuer aborts the mission with a reason (Preparing/EnRoute → MissionAborted). Incident is reset to Pending and a new rescue session is created with increased radius.")]
        [SwaggerResponse(200, "Mission aborted, new session created")]
        [SwaggerResponse(400, "Invalid status transition")]
        [SwaggerResponse(404, "Mission not found")]
        public async Task<IActionResult> AbortMission(
            Guid missionId,
            [FromBody] UpdateRescueMissionStatusRequest request)
        {
            await _missionService.RescuerAbortMissionAsync(missionId, request.CancellationReason ?? "No reason provided");
            return Ok(ApiResponseBuilder.BuildSuccessResponse<object>(null, "Mission aborted. New rescue session created with increased radius."));
        }

        /// <summary>
        /// Cancel mission - user cancels before rescuer starts
        /// No new session is created
        /// </summary>
        [HttpPatch("{missionId}/cancel")]
        [SwaggerOperation(
            Summary = "Cancel Mission (User)",
            Description = "User cancels the mission before rescuer goes en route (Preparing → Cancelled). Incident is set to Cancelled. No new session is created.")]
        [SwaggerResponse(200, "Mission cancelled")]
        [SwaggerResponse(400, "Invalid status transition - can only cancel during Preparing phase")]
        [SwaggerResponse(404, "Mission not found")]
        public async Task<IActionResult> CancelMission(
            Guid missionId,
            [FromBody] UpdateRescueMissionStatusRequest request)
        {
            await _missionService.UserCancelMissionAsync(missionId, request.CancellationReason ?? "No reason provided");
            return Ok(ApiResponseBuilder.BuildSuccessResponse<object>(null, "Mission cancelled by user."));
        }
    }
}
