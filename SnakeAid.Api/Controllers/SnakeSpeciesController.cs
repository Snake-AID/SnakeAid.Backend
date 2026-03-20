using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
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
        [SwaggerOperation(Summary = "Get Snake Species by ID", Description = "Get detailed information of a specific snake species including alternative names, antivenoms, and venoms")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<DetailSnakeSpeciesResponse>))]
        [SwaggerResponse(404, "Snake species not found")]
        public async Task<IActionResult> GetSnakeSpeciesById(int id)
        {
            var result = await _snakeSpeciesService.GetSnakeSpeciesByIdAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Create snake species from excel file and image file
        /// </summary>
        [HttpPost("import-excel")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create Snake Species From Excel", Description = "Upload excel (4 sheets) and image file. Sheet1: basic fields, Sheet2: Identification, Sheet3: SymptomsByTime, Sheet4: FirstAidGuidelineOverride")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<DetailSnakeSpeciesResponse>))]
        [SwaggerResponse(422, "Validation error")]
        public async Task<IActionResult> CreateSnakeSpeciesFromExcel([FromForm] CreateSnakeSpeciesFromExcelRequest request, CancellationToken ct)
        {
            var result = await _snakeSpeciesService.CreateSnakeSpeciesFromExcelAsync(request, User, ct);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Snake species created successfully from excel."));
        }
    }
}
