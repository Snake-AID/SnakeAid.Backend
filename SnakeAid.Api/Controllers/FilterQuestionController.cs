using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.FilterQuestion;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/filter-questions")]
    [ApiController]
    public class FilterQuestionController : BaseController<FilterQuestionController>
    {
        private readonly IFilterQuestionService _filterQuestionService;

        public FilterQuestionController(
            ILogger<FilterQuestionController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IFilterQuestionService filterQuestionService)
            : base(logger, httpContextAccessor, mapper)
        {
            _filterQuestionService = filterQuestionService;
        }

        /// <summary>
        /// Get all active filter questions with options for snake identification questionnaire
        /// </summary>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get All Filter Questions",
            Description = "Retrieve all active filter questions with their options. Used for snake identification questionnaire flow."
        )]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<FilterQuestionResponse>>))]
        public async Task<IActionResult> GetAllFilterQuestions()
        {
            var result = await _filterQuestionService.GetAllFilterQuestionsAsync();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }
    }
}
