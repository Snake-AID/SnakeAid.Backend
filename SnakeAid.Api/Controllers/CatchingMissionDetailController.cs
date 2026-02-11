using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SnakeCatchingMission;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/catching-mission-details")]
    [ApiController]
    [Authorize]
    public class CatchingMissionDetailController : BaseController<CatchingMissionDetailController>
    {
        private readonly ICatchingMissionDetailService _catchingMissionDetailService;

        public CatchingMissionDetailController(
            ILogger<CatchingMissionDetailController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ICatchingMissionDetailService catchingMissionDetailService)
            : base(logger, httpContextAccessor, mapper)
        {
            _catchingMissionDetailService = catchingMissionDetailService;
        }

        /// <summary>
        /// Create a new catching mission detail
        /// </summary>
        /// <remarks>
        /// Records the details of snakes caught during a mission, including species and quantity.
        /// This should be called when recording the results of a catching mission.
        /// </remarks>
        [HttpPost]
        [ValidateModel]
        [SwaggerOperation(
            Summary = "Create Catching Mission Detail",
            Description = "Create a new catching mission detail record for snakes caught during a mission")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<CatchingMissionDetailResponse>))]
        [SwaggerResponse(400, "Validation error or invalid data")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Mission or snake species not found")]
        public async Task<IActionResult> CreateCatchingMissionDetail([FromBody] CreateCatchingMissionDetailRequest request)
        {
            var result = await _catchingMissionDetailService.CreateCatchingMissionDetailAsync(request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Catching mission detail created successfully!"));
        }
    }
}
