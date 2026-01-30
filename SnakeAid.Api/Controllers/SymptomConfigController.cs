using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SymptomConfig;
using SnakeAid.Core.Responses.SymptomConfig;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/symptom-configs")]
    [ApiController]
    public class SymptomConfigController : BaseController<SymptomConfigController>
    {
        private readonly ISymptomConfigService _symptomConfigService;

        public SymptomConfigController(
            ILogger<SymptomConfigController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ISymptomConfigService symptomConfigService)
            : base(logger, httpContextAccessor, mapper)
        {
            _symptomConfigService = symptomConfigService;
        }

        /// <summary>
        /// Create a new symptom configuration
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create Symptom Configuration", Description = "Create a new symptom configuration (Admin only)")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<SymptomConfigResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "VenomType not found")]
        public async Task<IActionResult> CreateSymptomConfig([FromBody] CreateSymptomConfigRequest request)
        {
            var result = await _symptomConfigService.CreateSymptomConfigAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get symptom configuration by ID
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get Symptom Configuration by ID", Description = "Get detailed information of a symptom configuration")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<SymptomConfigResponse>))]
        [SwaggerResponse(404, "Configuration not found")]
        public async Task<IActionResult> GetSymptomConfigById(int id)
        {
            var result = await _symptomConfigService.GetSymptomConfigByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get list of symptom configurations with pagination and filters
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Filter Symptom Configurations", Description = "Get paginated list of symptom configurations with optional filters")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedData<SymptomConfigResponse>>))]
        public async Task<IActionResult> FilterSymptomConfigs([FromQuery] GetSymptomConfigRequest request)
        {
            var result = await _symptomConfigService.FilterSymptomConfigsAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get all symptom configurations without pagination
        /// </summary>
        [HttpGet("all")]
        [SwaggerOperation(Summary = "Get All Symptom Configurations", Description = "Get all symptom configurations without pagination")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<SymptomConfigResponse>>))]
        public async Task<IActionResult> GetAllSymptomConfig()
        {
            var result = await _symptomConfigService.GetAllSymptomConfigAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get symptom configurations grouped by AttributeKey
        /// </summary>
        [HttpGet("grouped")]
        [SwaggerOperation(Summary = "Get Symptom Configurations Grouped", Description = "Get active symptom configurations grouped by AttributeKey")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<Dictionary<string, List<SymptomConfigResponse>>>))]
        public async Task<IActionResult> GetSymptomConfigsGrouped()
        {
            var result = await _symptomConfigService.GetSymptomConfigsGroupedByKeyAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Update an existing symptom configuration
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update Symptom Configuration", Description = "Update an existing symptom configuration (Admin only)")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<SymptomConfigResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Configuration or VenomType not found")]
        public async Task<IActionResult> UpdateSymptomConfig(int id, [FromBody] UpdateSymptomConfigRequest request)
        {
            var result = await _symptomConfigService.UpdateSymptomConfigAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Delete a symptom configuration
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete Symptom Configuration", Description = "Delete a symptom configuration (Admin only)")]
        [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<bool>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Configuration not found")]
        public async Task<IActionResult> DeleteSymptomConfig(int id)
        {
            var result = await _symptomConfigService.DeleteSymptomConfigAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}
