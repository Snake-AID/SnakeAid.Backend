using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.FilterQuestion;
using SnakeAid.Core.Requests.SnakeSpecies;
using SnakeAid.Core.Responses.SnakeSpecies;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/snake-species")]
    [ApiController]
    public class SnakeSpeciesController : BaseController<SnakeSpeciesController>
    {
        private readonly ISnakeSpeciesService _snakeSpeciesService;

        public SnakeSpeciesController(
            ILogger<SnakeSpeciesController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ISnakeSpeciesService snakeSpeciesService)
            : base(logger, httpContextAccessor, mapper)
        {
            _snakeSpeciesService = snakeSpeciesService;
        }

        /// <summary>
        /// Get all snake species
        /// </summary>
        [HttpGet]
        [Authorize]
        [SwaggerOperation(Summary = "Get All Snake Species", Description = "Get list of all active snake species")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<ListSnakeSpeciesResponse>>))]
        public async Task<IActionResult> GetAllSnakeSpecies()
        {
            var result = await _snakeSpeciesService.GetAllSnakeSpeciesAsync();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Get snake species details by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Get Snake Species by ID", Description = "Get detailed information of a specific snake species including alternative names, antivenoms, and venoms")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<DetailSnakeSpeciesResponse>))]
        [SwaggerResponse(404, "Snake species not found")]
        public async Task<IActionResult> GetSnakeSpeciesById(int id)
        {
            var result = await _snakeSpeciesService.GetSnakeSpeciesByIdAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Create snake species
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create Snake Species")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<DetailSnakeSpeciesResponse>))]
        [SwaggerResponse(422, "Validation error")]
        public async Task<IActionResult> CreateSnakeSpecies([FromBody] CreateSnakeSpeciesRequest request, CancellationToken ct)
        {
            var result = await _snakeSpeciesService.CreateSnakeSpeciesAsync(request, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Snake species created successfully."));
        }

        /// <summary>
        /// Update snake species
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update Snake Species")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<DetailSnakeSpeciesResponse>))]
        [SwaggerResponse(422, "Validation error")]
        public async Task<IActionResult> UpdateSnakeSpecies(int id, [FromBody] UpdateSnakeSpeciesRequest request, CancellationToken ct)
        {
            var result = await _snakeSpeciesService.UpdateSnakeSpeciesAsync(id, request, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Snake species updated successfully."));
        }

        /// <summary>
        /// Delete snake species
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete Snake Species")]
        [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<object>))]
        public async Task<IActionResult> DeleteSnakeSpecies(int id, CancellationToken ct)
        {
            await _snakeSpeciesService.DeleteSnakeSpeciesAsync(id, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse("Snake species deleted successfully."));
        }

        /// <summary>
        /// Create snake species from excel file and image file
        /// </summary>
        [HttpPost("create-with-file")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create Snake Species With File", Description = "Upload excel (7 sheets) and image file. Sheet1: basic fields, Sheet2: Identification, Sheet3: SymptomsByTime, Sheet4: FirstAidGuidelineOverride, Sheet5: AntiVenom, Sheet6: Venom, Sheet7: AlternativeName")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<DetailSnakeSpeciesResponse>))]
        [SwaggerResponse(422, "Validation error")]
        public async Task<IActionResult> CreateWithFile([FromForm] CreateSnakeSpeciesWithFileRequest request, CancellationToken ct)
        {
            var result = await _snakeSpeciesService.CreateSnakeSpeciesWithFileAsync(request, User, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Snake species created successfully with file."));
        }

        /// <summary>
        /// Search snake species with venom and antivenom data for expert consultation
        /// </summary>
        [HttpGet("search")]
        [Authorize]
        [SwaggerOperation(Summary = "Search Snake Species", Description = "Search snake species by text query, including venom types and available antivenoms for expert reference during consultations")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<SearchSnakeSpeciesResponse>>))]
        public async Task<IActionResult> SearchSnakes([FromQuery] string q)
        {
            var result = await _snakeSpeciesService.SearchSnakeSpeciesAsync(q);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Filter snake species by questionnaire answers
        /// </summary>
        [HttpPost("filter-by-answers")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Filter Snakes by Questionnaire Answers",
            Description = "Filter snake species based on user's answers to identification questionnaire. Returns snakes sorted by match score (best matches first)."
        )]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<FilteredSnakeResponse>>))]
        [SwaggerResponse(400, "Invalid request - no options selected")]
        public async Task<IActionResult> FilterSnakesByAnswers(
            [FromBody] FilterSnakeByAnswersRequest request,
            CancellationToken ct)
        {
            var result = await _snakeSpeciesService.FilterSnakesByAnswersAsync(
                request.SelectedOptionIds,
                ct
            );
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Get snakes by GPS location (location-based filtering)
        /// </summary>
        [HttpGet("by-location")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Get Snakes by GPS Location",
            Description = "Get list of snake species common in the geographic region based on GPS coordinates. Returns snakes sorted by priority (most common first)."
        )]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<SnakesByLocationResponse>))]
        [SwaggerResponse(400, "Invalid coordinates")]
        [SwaggerResponse(404, "No region found for this location")]
        public async Task<IActionResult> GetSnakesByLocation(
            [FromQuery] double lat,
            [FromQuery] double lng,
            CancellationToken ct)
        {
            var result = await _snakeSpeciesService.GetSnakesByLocationAsync(lat, lng, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        // ── Geographic Regions ────────────────────────────────────────────────

        /// <summary>
        /// Get all geographic regions with polygon boundaries for map rendering
        /// </summary>
        [HttpGet("/api/geographic-regions")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Get All Geographic Regions",
            Description = "Returns all active regions with polygon boundary coordinates (GeoJSON [lng,lat] order). Geometry-only payload for map initialization/cache."
        )]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<GeographicRegionResponse>>))]
        public async Task<IActionResult> GetAllRegions(CancellationToken ct)
        {
            var result = await _snakeSpeciesService.GetAllRegionsAsync(ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        // ── Region-Snake Mappings ─────────────────────────────────────────────

        /// <summary>
        /// Get all region distribution mappings for a snake species
        /// </summary>
        [HttpGet("{id}/region-mappings")]
        [Authorize]
        [SwaggerOperation(Summary = "Get Region Mappings for Snake", Description = "Returns all geographic regions this snake species is mapped to, with commonality and priority metadata.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<RegionSnakeMappingResponse>>))]
        [SwaggerResponse(404, "Snake species not found")]
        public async Task<IActionResult> GetRegionMappings(int id, CancellationToken ct)
        {
            var result = await _snakeSpeciesService.GetRegionMappingsBySnakeAsync(id, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Sync (replace-all) region mappings for a snake species
        /// </summary>
        [HttpPut("{id}/region-mappings")]
        [Authorize]
        [ValidateModel]
        [SwaggerOperation(
            Summary = "Sync Region Mappings",
            Description = "Replaces all region mappings for this snake species with the provided list. Regions not in the list will be removed."
        )]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<RegionSnakeMappingResponse>>))]
        [SwaggerResponse(404, "Snake species or region not found")]
        public async Task<IActionResult> SyncRegionMappings(int id, [FromBody] SyncRegionMappingsRequest request, CancellationToken ct)
        {
            var result = await _snakeSpeciesService.SyncRegionMappingsAsync(id, request, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Region mappings updated successfully."));
        }

        /// <summary>
        /// Add a single region mapping to a snake species
        /// </summary>
        [HttpPost("{id}/region-mappings")]
        [Authorize]
        [ValidateModel]
        [SwaggerOperation(Summary = "Add Region Mapping", Description = "Adds a single geographic region distribution mapping for this snake species.")]
        [SwaggerResponse(200, "Created", typeof(ApiResponse<RegionSnakeMappingResponse>))]
        [SwaggerResponse(400, "Duplicate mapping")]
        [SwaggerResponse(404, "Snake species or region not found")]
        public async Task<IActionResult> AddRegionMapping(int id, [FromBody] AddRegionMappingRequest request, CancellationToken ct)
        {
            var result = await _snakeSpeciesService.AddRegionMappingAsync(id, request, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Region mapping added successfully."));
        }

        /// <summary>
        /// Update an existing region mapping
        /// </summary>
        [HttpPatch("{id}/region-mappings/{mappingId}")]
        [Authorize]
        [SwaggerOperation(Summary = "Update Region Mapping", Description = "Updates commonality level, priority, or distribution notes for an existing region mapping.")]
        [SwaggerResponse(200, "Updated", typeof(ApiResponse<RegionSnakeMappingResponse>))]
        [SwaggerResponse(404, "Mapping not found")]
        public async Task<IActionResult> UpdateRegionMapping(int id, int mappingId, [FromBody] UpdateRegionMappingRequest request, CancellationToken ct)
        {
            var result = await _snakeSpeciesService.UpdateRegionMappingAsync(id, mappingId, request, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Region mapping updated successfully."));
        }

        /// <summary>
        /// Delete a region mapping
        /// </summary>
        [HttpDelete("{id}/region-mappings/{mappingId}")]
        [Authorize]
        [SwaggerOperation(Summary = "Delete Region Mapping")]
        [SwaggerResponse(200, "Deleted")]
        [SwaggerResponse(404, "Mapping not found")]
        public async Task<IActionResult> DeleteRegionMapping(int id, int mappingId, CancellationToken ct)
        {
            await _snakeSpeciesService.DeleteRegionMappingAsync(id, mappingId, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse("Region mapping deleted successfully."));
        }
    }
}
