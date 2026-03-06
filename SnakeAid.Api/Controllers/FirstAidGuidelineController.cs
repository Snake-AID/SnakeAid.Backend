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
        private readonly IFirstAidRecommendationService _recommendationService;

        public FirstAidGuidelineController(
            ILogger<FirstAidGuidelineController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IFirstAidGuidelineService guidelineService,
            IFirstAidRecommendationService recommendationService)
            : base(logger, httpContextAccessor, mapper)
        {
            _guidelineService = guidelineService;
            _recommendationService = recommendationService;
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
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "First aid guideline created successfully!"));
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
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
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
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
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
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
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
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
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
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "First aid guideline updated successfully!"));
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
            await _guidelineService.DeleteFirstAidGuidelineAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse("First aid guideline deleted successfully!"));
        }

        /// <summary>
        /// Get recommended first aid guideline for a snakebite incident
        /// </summary>
        [HttpGet("recommendation/incident/{incidentId}")]
        [SwaggerOperation(Summary = "Get Recommendation for Incident", Description = "Get the recommended first aid guideline for a specific snakebite incident based on identified snake")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<Core.Responses.FirstAid.FirstAidRecommendationResponse>))]
        [SwaggerResponse(404, "Incident not found")]
        public async Task<IActionResult> GetRecommendationForIncident(Guid incidentId)
        {
            var result = await _recommendationService.GetRecommendationForIncidentAsync(incidentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Get first aid guideline recommendation for a specific snake species
        /// </summary>
        [HttpGet("recommendation/species/{snakeSpeciesId}")]
        [SwaggerOperation(Summary = "Get Recommendation for Species", Description = "Get the first aid guideline recommendation for a specific snake species")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<Core.Responses.FirstAid.FirstAidRecommendationResponse>))]
        [SwaggerResponse(404, "Snake species not found")]
        public async Task<IActionResult> GetRecommendationForSpecies(int snakeSpeciesId)
        {
            var result = await _recommendationService.GetRecommendationForSpeciesAsync(snakeSpeciesId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Get general first aid guideline for snake bites (when snake is not identified)
        /// </summary>
        [HttpGet("recommendation/general")]
        [SwaggerOperation(Summary = "Get General Recommendation", Description = "Get general first aid guideline for snake bites when the snake species is not identified")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<Core.Responses.FirstAid.FirstAidRecommendationResponse>))]
        public async Task<IActionResult> GetGeneralRecommendation()
        {
            var result = await _recommendationService.GetGeneralRecommendationAsync();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }
    }
}
