using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/monitoring")]
    [ApiController]
    public class OperatorController : BaseController<OperatorController>
    {
        private readonly IOperatorSnapshotService _operatorSnapshotService;

        public OperatorController(
            ILogger<OperatorController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IOperatorSnapshotService operatorSnapshotService)
            : base(logger, httpContextAccessor, mapper)
        {
            _operatorSnapshotService = operatorSnapshotService;
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
    }
}
