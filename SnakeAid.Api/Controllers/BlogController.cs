using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Blog;
using SnakeAid.Core.Responses.Blog;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/blogs")]
    [ApiController]
    public class BlogController : BaseController<BlogController>
    {
        private readonly IBlogService _blogService;

        public BlogController(
            ILogger<BlogController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IBlogService blogService)
            : base(logger, httpContextAccessor, mapper)
        {
            _blogService = blogService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Expert")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create blog")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<BlogResponse>))]
        public async Task<IActionResult> CreateBlog([FromBody] CreateBlogRequest request)
        {
            var authorId = GetCurrentUserId();
            var result = await _blogService.CreateBlogAsync(request, authorId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Blog created successfully."));
        }

        [HttpGet]
        [Authorize]
        [SwaggerOperation(Summary = "Get blogs with filter")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<BlogResponse>>))]
        public async Task<IActionResult> GetBlogs([FromQuery] GetBlogsRequest request)
        {
            var result = await _blogService.GetBlogsAsync(request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet("{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Get blog detail")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<BlogResponse>))]
        [SwaggerResponse(404, "Not found")]
        public async Task<IActionResult> GetBlogById([FromRoute] Guid id)
        {
            var result = await _blogService.GetBlogByIdAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, Expert")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update blog")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<BlogResponse>))]
        [SwaggerResponse(404, "Not found")]
        public async Task<IActionResult> UpdateBlog([FromRoute] Guid id, [FromBody] UpdateBlogRequest request)
        {
            var result = await _blogService.UpdateBlogAsync(id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Blog updated successfully."));
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin, Expert")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update blog status")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<BlogResponse>))]
        [SwaggerResponse(404, "Not found")]
        public async Task<IActionResult> UpdateBlogStatus([FromRoute] Guid id, [FromBody] UpdateBlogStatusRequest request)
        {
            var result = await _blogService.UpdateBlogStatusAsync(id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Blog status updated successfully."));
        }

        [HttpPatch("{id}/view")]
        [Authorize]
        [SwaggerOperation(Summary = "Increase blog view")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<BlogResponse>))]
        [SwaggerResponse(404, "Not found")]
        public async Task<IActionResult> ViewBlog([FromRoute] Guid id)
        {
            var result = await _blogService.IncreaseViewAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Blog view increased successfully."));
        }

        [HttpPatch("{id}/like")]
        [Authorize]
        [SwaggerOperation(Summary = "Like blog")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<BlogResponse>))]
        [SwaggerResponse(404, "Not found")]
        public async Task<IActionResult> LikeBlog([FromRoute] Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _blogService.LikeBlogAsync(id, userId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Blog liked successfully."));
        }

        [HttpPatch("{id}/unlike")]
        [Authorize]
        [SwaggerOperation(Summary = "Unlike blog")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<BlogResponse>))]
        [SwaggerResponse(404, "Not found")]
        public async Task<IActionResult> UnlikeBlog([FromRoute] Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _blogService.UnlikeBlogAsync(id, userId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Blog unliked successfully."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, Expert")]
        [SwaggerOperation(Summary = "Delete blog")]
        [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<bool>))]
        [SwaggerResponse(404, "Not found")]
        public async Task<IActionResult> DeleteBlog([FromRoute] Guid id)
        {
            await _blogService.DeleteBlogAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(true, "Blog deleted successfully."));
        }
    }
}
