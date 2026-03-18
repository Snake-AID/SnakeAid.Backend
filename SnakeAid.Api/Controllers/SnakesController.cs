using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.SnakeSpecies;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/v1/snakes")]
    [ApiController]
    public class SnakesController : BaseController<SnakesController>
    {
        private readonly ISnakeSpeciesService _snakeSpeciesService;

        public SnakesController(
            ILogger<SnakesController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ISnakeSpeciesService snakeSpeciesService)
            : base(logger, httpContextAccessor, mapper)
        {
            _snakeSpeciesService = snakeSpeciesService;
        }

        /// <summary>
        /// Search snake species with venom and antivenom data for expert consultation
        /// </summary>
        [HttpGet("search")]
        [SwaggerOperation(Summary = "Search Snake Species", Description = "Search snake species by text query, including venom types and available antivenoms for expert reference during consultations")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<SearchSnakeSpeciesResponse>>))]
        public async Task<IActionResult> SearchSnakes([FromQuery] string q)
        {
            var result = await _snakeSpeciesService.SearchSnakeSpeciesAsync(q);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }
    }
}