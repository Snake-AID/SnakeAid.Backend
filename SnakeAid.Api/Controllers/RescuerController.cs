using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/rescuers")]
    [ApiController]
    [Authorize(Roles = "Operator,Admin")]
    public class RescuerController : BaseController<RescuerController>
    {
        private readonly IOperatorSnapshotService _operatorSnapshotService;

        public RescuerController(
            ILogger<RescuerController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IOperatorSnapshotService operatorSnapshotService)
            : base(logger, httpContextAccessor, mapper)
        {
            _operatorSnapshotService = operatorSnapshotService;
        }

        [HttpGet("online")]
        [SwaggerOperation(
            Summary = "Get Online Rescuers",
            Description = "Retrieve all rescuers currently marked as online.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<BriefRescuerProfileResponse>>))]
        public async Task<IActionResult> GetOnlineRescuers()
        {
            var result = await _operatorSnapshotService.GetOnlineRescuersAsync();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
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
    }
}