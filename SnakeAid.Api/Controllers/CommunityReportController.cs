using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.CommunityReport;
using SnakeAid.Core.Responses.CommunityReport;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/community-reports")]
    [ApiController]
    [Authorize]
    public class CommunityReportController : BaseController<CommunityReportController>
    {
        private readonly ICommunityReportService _communityReportService;

        public CommunityReportController(
            ILogger<CommunityReportController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ICommunityReportService communityReportService)
            : base(logger, httpContextAccessor, mapper)
        {
            _communityReportService = communityReportService;
        }

        [HttpPost]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create community report")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<CommunityReportResponse>))]
        public async Task<IActionResult> CreateCommunityReport([FromBody] CreateCommunityReportRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _communityReportService.CreateCommunityReportAsync(request, userId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Community report created successfully."));
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get community reports")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<CommunityReportResponse>>))]
        public async Task<IActionResult> GetCommunityReports()
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var result = await _communityReportService.GetCommunityReportsAsync(userId, userRole);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get community report by id")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<CommunityReportResponse>))]
        [SwaggerResponse(404, "Not found")]
        public async Task<IActionResult> GetCommunityReportById([FromRoute] Guid id)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var result = await _communityReportService.GetCommunityReportByIdAsync(id, userId, userRole);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpPut("{id}")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update community report")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<CommunityReportResponse>))]
        [SwaggerResponse(404, "Not found")]
        public async Task<IActionResult> UpdateCommunityReport([FromRoute] Guid id, [FromBody] UpdateCommunityReportRequest request)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var result = await _communityReportService.UpdateCommunityReportAsync(id, request, userId, userRole);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Community report updated successfully."));
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete community report")]
        [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<bool>))]
        [SwaggerResponse(404, "Not found")]
        public async Task<IActionResult> DeleteCommunityReport([FromRoute] Guid id)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            await _communityReportService.DeleteCommunityReportAsync(id, userId, userRole);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(true, "Community report deleted successfully."));
        }
    }
}
