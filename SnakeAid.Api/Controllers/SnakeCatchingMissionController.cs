using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SnakeCatchingMission;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using SnakeAid.Core.Utils;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SnakeAid.Api.Controllers
{
    [Authorize]
    [Route("api/snake-catching-missions")]
    [ApiController]
    public class SnakeCatchingMissionController : ControllerBase
    {
        private readonly ISnakeCatchingMissionService _missionService;
        private readonly ILogger<SnakeCatchingMissionController> _logger;

        public SnakeCatchingMissionController(
            ISnakeCatchingMissionService missionService,
            ILogger<SnakeCatchingMissionController> logger)
        {
            _missionService = missionService;
            _logger = logger;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        /// <summary>
        /// Start mission - Update status to EnRoute
        /// </summary>
        /// <param name="missionId">The ID of the mission</param>
        /// <param name="request">Optional notes</param>
        [HttpPut("{missionId:guid}/start")]
        [SwaggerOperation(
            Summary = "Start Mission (EnRoute)",
            Description = "Rescuer starts the mission and updates status from Preparing to EnRoute")]
        [SwaggerResponse(200, "Mission started successfully", typeof(ApiResponse<SnakeCatchingMissionDetailResponse>))]
        [SwaggerResponse(400, "Invalid status transition")]
        [SwaggerResponse(401, "User not authenticated")]
        [SwaggerResponse(404, "Mission not found")]
        [ProducesResponseType(typeof(ApiResponse<SnakeCatchingMissionDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StartMission(
            [FromRoute] Guid missionId,
            [FromBody] UpdateMissionStatusRequest request)
        {
            var rescuerId = GetCurrentUserId();
            var result = await _missionService.StartMissionAsync(rescuerId, missionId, request);

            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                result,
                "Mission started successfully. Status updated to EnRoute."));
        }

        /// <summary>
        /// Mark mission as arrived - Update status to Arrived
        /// </summary>
        /// <param name="missionId">The ID of the mission</param>
        /// <param name="request">Optional notes</param>
        [HttpPut("{missionId:guid}/arrived")]
        [SwaggerOperation(
            Summary = "Mark Mission as Arrived",
            Description = "Rescuer marks arrival at location and updates status from EnRoute to Arrived")]
        [SwaggerResponse(200, "Mission marked as arrived", typeof(ApiResponse<SnakeCatchingMissionDetailResponse>))]
        [SwaggerResponse(400, "Invalid status transition")]
        [SwaggerResponse(401, "User not authenticated")]
        [SwaggerResponse(404, "Mission not found")]
        [ProducesResponseType(typeof(ApiResponse<SnakeCatchingMissionDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsArrived(
            [FromRoute] Guid missionId,
            [FromBody] UpdateMissionStatusRequest request)
        {
            var rescuerId = GetCurrentUserId();
            var result = await _missionService.MarkAsArrivedAsync(rescuerId, missionId, request);

            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                result,
                "Mission marked as arrived successfully."));
        }
    }
}
