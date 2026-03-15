using System;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.Shift;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/monitoring")]
    [ApiController]
    [Authorize(Roles = "Operator,Admin")]
    public class OperatorController : BaseController<OperatorController>
    {
        private readonly IOperatorSnapshotService _operatorSnapshotService;
        private readonly IShiftService _shiftService;

        public OperatorController(
            ILogger<OperatorController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IOperatorSnapshotService operatorSnapshotService,
            IShiftService shiftService)
            : base(logger, httpContextAccessor, mapper)
        {
            _operatorSnapshotService = operatorSnapshotService;
            _shiftService = shiftService;
        }

        /// <summary>Basic service health check</summary>
        [HttpGet("health")]
        [SwaggerOperation(Summary = "Health Check", Description = "Returns service health status")]
        [SwaggerResponse(200, "Service is healthy")]
        public IActionResult HealthCheck()
        {
            return Ok(new { Status = "Healthy", CheckedAt = DateTime.UtcNow });
        }

        [HttpGet("on-duty")]
        [SwaggerOperation(
        Summary = "Get On-Duty Rescuers Snapshot",
        Description = "Return snapshot data for operator map: on-duty rescuers, online/available status, shift info, and optional distance to incident.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<OnDutyRescuerSnapshotResponse>))]
        public async Task<IActionResult> GetOnDutyRescuers(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? incidentId,
        [FromQuery] bool onlyAvailable = true,
        [FromQuery] double? maxDistanceKm = null)
        {
            var result = await _operatorSnapshotService.GetOnDutyRescuersAsync(
                date,
                incidentId,
                onlyAvailable,
                maxDistanceKm);

            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet("rescuers")]
        [SwaggerOperation(
            Summary = "Get Rescuer Registry",
            Description = "Return a list of all rescuer profiles (registry) for operator dashboard.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<BriefRescuerProfileResponse>>))]
        public async Task<IActionResult> GetRescuerRegistry()
        {
            var result = await _operatorSnapshotService.GetRescuerRegistryAsync();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet("rescuers/{rescuerId:guid}")]
        [SwaggerOperation(
            Summary = "Get Rescuer by Id",
            Description = "Return rescuer profile details by rescuer id.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<BriefRescuerProfileResponse>))]
        [SwaggerResponse(404, "Not found", typeof(ApiResponse<object>))]
        public async Task<IActionResult> GetRescuerById(Guid rescuerId)
        {
            var result = await _operatorSnapshotService.GetRescuerByIdAsync(rescuerId);
            if (result == null)
                return NotFound(ApiResponseBuilder.BuildNotFoundResponse($"Rescuer {rescuerId} not found."));

            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet("shift-assignments/today")]
        [SwaggerOperation(
            Summary = "Get Today's Shift Assignments",
            Description = "Return all shift assignments for today (for operator dashboard).")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<ShiftAssignmentResponse>>))]
        public async Task<IActionResult> GetTodayShiftAssignments()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await _shiftService.GetAssignmentsByDateAsync(today);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }
    }
}
