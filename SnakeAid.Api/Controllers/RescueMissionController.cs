// using MapsterMapper;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
// using SnakeAid.Core.Domains;
// using SnakeAid.Core.Meta;
// using SnakeAid.Core.Requests.RescueMission;
// using SnakeAid.Core.Responses.RescueMission;
// using SnakeAid.Service.Interfaces;
// using Swashbuckle.AspNetCore.Annotations;
// using System;
// using System.Threading.Tasks;

// namespace SnakeAid.Api.Controllers
// {
//     [Route("api/rescue-missions")]
//     [ApiController]
//     [Authorize]
//     public class RescueMissionController : BaseController<RescueMissionController>
//     {
//         private readonly IRescueMissionService _missionService;

//         public RescueMissionController(
//             ILogger<RescueMissionController> logger,
//             IHttpContextAccessor httpContextAccessor,
//             IMapper mapper,
//             IRescueMissionService missionService)
//             : base(logger, httpContextAccessor, mapper)
//         {
//             _missionService = missionService;
//         }

//         /// <summary>
//         /// Get rescue mission details
//         /// </summary>
//         [HttpGet("{missionId}")]
//         [SwaggerOperation(Summary = "Get Mission Details", Description = "Retrieve detailed information about a rescue mission")]
//         [SwaggerResponse(200, "Mission details retrieved successfully", typeof(ApiResponse<RescueMissionStatusResponse>))]
//         [SwaggerResponse(404, "Mission not found")]
//         public async Task<IActionResult> GetMissionDetails(Guid missionId)
//         {
//             var result = await _missionService.GetMissionDetailsAsync(missionId);
//             return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Mission details retrieved successfully!"));
//         }

//         /// <summary>
//         /// Update rescue mission status
//         /// When status is set to MissionCompleted, the corresponding incident is automatically updated to Finished
//         /// </summary>
//         [HttpPatch("{missionId}/status")]
//         [SwaggerOperation(
//             Summary = "Update Mission Status",
//             Description = @"Update the status of a rescue mission. 

// Valid status transitions:
// - Preparing → EnRoute, Cancelled
// - EnRoute → RescuerArrived, MissionAborted
// - RescuerArrived → MissionCompleted, MissionUncompleted, MissionAborted

// When transitioning to MissionCompleted:
// - At least one verification image is required
// - The corresponding SnakebiteIncident status is automatically updated to Finished")]
//         [SwaggerResponse(200, "Mission status updated successfully", typeof(ApiResponse<RescueMissionStatusResponse>))]
//         [SwaggerResponse(400, "Invalid status transition or missing verification images")]
//         [SwaggerResponse(403, "Not authorized to update this mission")]
//         [SwaggerResponse(404, "Mission not found")]
//         [SwaggerResponse(422, "Validation error")]
//         public async Task<IActionResult> UpdateMissionStatus(
//             Guid missionId, 
//             [FromBody] UpdateRescueMissionStatusRequest request)
//         {
//             var rescuerId = GetCurrentUserId();
//             var result = await _missionService.UpdateMissionStatusAsync(missionId, request, rescuerId);

//             var message = request.Status == RescueMissionStatus.MissionCompleted
//                 ? "Mission completed successfully! Incident marked as finished."
//                 : $"Mission status updated to {request.Status} successfully!";

//             return Ok(ApiResponseBuilder.BuildSuccessResponse(result, message));
//         }

//         /// <summary>
//         /// Start mission - transition to EnRoute
//         /// </summary>
//         [HttpPatch("{missionId}/start")]
//         [SwaggerOperation(
//             Summary = "Start Mission", 
//             Description = "Start the rescue mission (Preparing → EnRoute). Rescuer begins heading to the incident location.")]
//         [SwaggerResponse(200, "Mission started successfully", typeof(ApiResponse<RescueMissionStatusResponse>))]
//         [SwaggerResponse(400, "Invalid status transition")]
//         [SwaggerResponse(403, "Not authorized")]
//         [SwaggerResponse(404, "Mission not found")]
//         public async Task<IActionResult> StartMission(Guid missionId)
//         {
//             var rescuerId = GetCurrentUserId();
//             var request = new UpdateRescueMissionStatusRequest { Status = RescueMissionStatus.EnRoute };
//             var result = await _missionService.UpdateMissionStatusAsync(missionId, request, rescuerId);
//             return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Mission started! En route to location."));
//         }

//         /// <summary>
//         /// Mark arrival at location - transition to RescuerArrived
//         /// </summary>
//         [HttpPatch("{missionId}/arrive")]
//         [SwaggerOperation(
//             Summary = "Arrive at Location",
//             Description = "Mark rescuer's arrival at the incident location (EnRoute → RescuerArrived).")]
//         [SwaggerResponse(200, "Arrival marked successfully", typeof(ApiResponse<RescueMissionStatusResponse>))]
//         [SwaggerResponse(400, "Invalid status transition")]
//         [SwaggerResponse(403, "Not authorized")]
//         [SwaggerResponse(404, "Mission not found")]
//         public async Task<IActionResult> ArriveAtLocation(Guid missionId)
//         {
//             var rescuerId = GetCurrentUserId();
//             var request = new UpdateRescueMissionStatusRequest { Status = RescueMissionStatus.RescuerArrived };
//             var result = await _missionService.UpdateMissionStatusAsync(missionId, request, rescuerId);
//             return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Arrival marked successfully!"));
//         }

//         /// <summary>
//         /// Complete mission - transition to MissionCompleted
//         /// Requires verification images and updates incident to Finished
//         /// </summary>
//         [HttpPatch("{missionId}/complete")]
//         [SwaggerOperation(
//             Summary = "Complete Mission",
//             Description = "Complete the rescue mission with verification images (RescuerArrived → MissionCompleted). Automatically updates incident status to Finished. Requires at least one verification image.")]
//         [SwaggerResponse(200, "Mission completed successfully", typeof(ApiResponse<RescueMissionStatusResponse>))]
//         [SwaggerResponse(400, "Invalid status transition or missing verification images")]
//         [SwaggerResponse(403, "Not authorized")]
//         [SwaggerResponse(404, "Mission not found")]
//         [SwaggerResponse(422, "Validation error")]
//         public async Task<IActionResult> CompleteMission(
//             Guid missionId,
//             [FromBody] UpdateRescueMissionStatusRequest request)
//         {
//             var rescuerId = GetCurrentUserId();
//             request.Status = RescueMissionStatus.MissionCompleted;
//             var result = await _missionService.UpdateMissionStatusAsync(missionId, request, rescuerId);
//             return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Mission completed successfully! Incident marked as finished."));
//         }

//         /// <summary>
//         /// Abort mission - transition to MissionAborted
//         /// </summary>
//         [HttpPatch("{missionId}/abort")]
//         [SwaggerOperation(
//             Summary = "Abort Mission",
//             Description = "Abort the mission with a reason (EnRoute/RescuerArrived → MissionAborted).")]
//         [SwaggerResponse(200, "Mission aborted", typeof(ApiResponse<RescueMissionStatusResponse>))]
//         [SwaggerResponse(400, "Invalid status transition")]
//         [SwaggerResponse(403, "Not authorized")]
//         [SwaggerResponse(404, "Mission not found")]
//         public async Task<IActionResult> AbortMission(
//             Guid missionId,
//             [FromBody] UpdateRescueMissionStatusRequest request)
//         {
//             var rescuerId = GetCurrentUserId();
//             request.Status = RescueMissionStatus.MissionAborted;
//             var result = await _missionService.UpdateMissionStatusAsync(missionId, request, rescuerId);
//             return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Mission aborted."));
//         }

//         /// <summary>
//         /// Cancel mission - transition to Cancelled
//         /// </summary>
//         [HttpPatch("{missionId}/cancel")]
//         [SwaggerOperation(
//             Summary = "Cancel Mission",
//             Description = "Cancel the mission before it starts (Preparing → Cancelled).")]
//         [SwaggerResponse(200, "Mission cancelled", typeof(ApiResponse<RescueMissionStatusResponse>))]
//         [SwaggerResponse(400, "Invalid status transition")]
//         [SwaggerResponse(403, "Not authorized")]
//         [SwaggerResponse(404, "Mission not found")]
//         public async Task<IActionResult> CancelMission(
//             Guid missionId,
//             [FromBody] UpdateRescueMissionStatusRequest request)
//         {
//             var rescuerId = GetCurrentUserId();
//             request.Status = RescueMissionStatus.Cancelled;
//             var result = await _missionService.UpdateMissionStatusAsync(missionId, request, rescuerId);
//             return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Mission cancelled."));
//         }
//     }
// }
