using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Lesson;
using SnakeAid.Core.Responses.Lesson;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/lessons")]
    [ApiController]
    public class LessonController : BaseController<LessonController>
    {
        private readonly ILessonService _lessonService;

        public LessonController(
            ILogger<LessonController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILessonService lessonService)
            : base(logger, httpContextAccessor, mapper)
        {
            _lessonService = lessonService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create Lesson", Description = "Create a new lesson")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<LessonResponse>))]
        [SwaggerResponse(400, "Validation error")]
        public async Task<IActionResult> CreateLesson([FromBody] CreateLessonRequest request)
        {
            var result = await _lessonService.CreateLessonAsync(request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Lesson created successfully!"));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Rescuer")]
        [SwaggerOperation(Summary = "Get Lesson by ID", Description = "Get detailed information of a lesson")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<LessonResponse>))]
        [SwaggerResponse(404, "Lesson not found")]
        public async Task<IActionResult> GetLessonById(Guid id)
        {
            var result = await _lessonService.GetLessonByIdAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet]
        [Authorize(Roles = "Admin, Rescuer")]
        [SwaggerOperation(Summary = "Get All Lessons", Description = "Get all lessons")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<LessonResponse>>))]
        public async Task<IActionResult> GetAllLessons()
        {
            var result = await _lessonService.GetAllLessonsAsync();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update Lesson", Description = "Update an existing lesson")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<LessonResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(404, "Lesson not found")]
        public async Task<IActionResult> UpdateLesson(Guid id, [FromBody] UpdateLessonRequest request)
        {
            var result = await _lessonService.UpdateLessonAsync(id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Lesson updated successfully!"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete Lesson", Description = "Delete a lesson")]
        [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<bool>))]
        [SwaggerResponse(404, "Lesson not found")]
        public async Task<IActionResult> DeleteLesson(Guid id)
        {
            var result = await _lessonService.DeleteLessonAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Lesson deleted successfully!"));
        }
    }
}
