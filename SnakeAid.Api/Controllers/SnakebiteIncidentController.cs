using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/incidents")]
    [ApiController]
    [Authorize]
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
        /// Create first snakebite incident report
        /// </summary>
        [HttpPost("sos")]
        [SwaggerOperation(Summary = "Create Snakebite Incident", Description = "Report Snakebite Incident Emergency Rescue")]
        [SwaggerResponse(200, "Create successful", typeof(ApiResponse<CreateIncidentResponse>))]
        [SwaggerResponse(400, "Member profile not found")]
        [SwaggerResponse(422, "Validation error")]
        public async Task<IActionResult> CreateSnakebiteIncident([FromBody] CreateIncidentRequest request)
        {
            var userId = GetCurrentUserId();

            var result = await _incidentService.CreateIncidentAsync(request, userId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
