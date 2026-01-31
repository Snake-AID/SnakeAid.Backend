using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.FirstAidGuideline;
using SnakeAid.Core.Responses.FirstAidGuideline;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/first-aid-guidelines")]
    [ApiController]
    public class FirstAidGuidelineController : BaseController<FirstAidGuidelineController>
    {
        private readonly IFirstAidGuidelineService _guidelineService;

        public FirstAidGuidelineController(
            ILogger<FirstAidGuidelineController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IFirstAidGuidelineService guidelineService)
            : base(logger, httpContextAccessor, mapper)
        {
            _guidelineService = guidelineService;
        }

        /// <summary>
        /// Create a new first aid guideline
        /// </summary>
        [HttpPost]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create First Aid Guideline", Description = "Create a new first aid guideline (Admin only)")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<FirstAidGuidelineResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        public async Task<IActionResult> CreateFirstAidGuideline([FromBody] CreateFirstAidGuidelineRequest request)
        {
            var result = await _guidelineService.CreateFirstAidGuidelineAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get first aid guideline by ID
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get First Aid Guideline by ID", Description = "Get detailed information of a first aid guideline")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<FirstAidGuidelineResponse>))]
        [SwaggerResponse(404, "Guideline not found")]
        public async Task<IActionResult> GetFirstAidGuidelineById(int id)
        {
            var result = await _guidelineService.GetFirstAidGuidelineByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get list of first aid guidelines with pagination and filters
        /// </summary>
        [HttpGet("filter")]
        [SwaggerOperation(Summary = "Filter First Aid Guidelines", Description = "Get paginated list of first aid guidelines with optional filters")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedData<FirstAidGuidelineResponse>>))]
        public async Task<IActionResult> FilterFirstAidGuidelines([FromQuery] GetFirstAidGuidelineRequest request)
        {
            var result = await _guidelineService.FilterFirstAidGuidelinesAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get all first aid guidelines without pagination
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get All First Aid Guidelines", Description = "Get all first aid guidelines without pagination")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<FirstAidGuidelineResponse>>))]
        public async Task<IActionResult> GetAllFirstAidGuideline()
        {
            var result = await _guidelineService.GetAllFirstAidGuidelineAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get first aid guidelines by snake species ID
        /// </summary>
        [HttpGet("by-snake-species/{snakeSpeciesId}")]
        [SwaggerOperation(Summary = "Get First Aid Guidelines by Snake Species", Description = "Get first aid guidelines based on the venom types of a specific snake species")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<FirstAidGuidelineResponse>>))]
        [SwaggerResponse(404, "Snake species not found")]
        public async Task<IActionResult> GetFirstAidGuidelinesBySnakeSpecies(int snakeSpeciesId)
        {
            var result = await _guidelineService.GetFirstAidGuidelinesBySnakeSpeciesIdAsync(snakeSpeciesId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Update an existing first aid guideline
        /// </summary>
        [HttpPut("{id}")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update First Aid Guideline", Description = "Update an existing first aid guideline (Admin only)")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<FirstAidGuidelineResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Guideline not found")]
        public async Task<IActionResult> UpdateFirstAidGuideline(int id, [FromBody] UpdateFirstAidGuidelineRequest request)
        {
            var result = await _guidelineService.UpdateFirstAidGuidelineAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Delete a first aid guideline
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete First Aid Guideline", Description = "Delete a first aid guideline (Admin only)")]
        [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<bool>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Guideline not found")]
        public async Task<IActionResult> DeleteFirstAidGuideline(int id)
        {
            var result = await _guidelineService.DeleteFirstAidGuidelineAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}
