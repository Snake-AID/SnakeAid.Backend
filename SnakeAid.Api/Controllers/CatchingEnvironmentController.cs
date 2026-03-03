using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.CatchingEnvironment;
using SnakeAid.Core.Responses.CatchingEnvironment;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/catchingenvironments")]
    [ApiController]
    public class CatchingEnvironmentController : BaseController<CatchingEnvironmentController>
    {
        private readonly ICatchingEnvironmentService _catchingEnvironmentService;

        public CatchingEnvironmentController(
            ILogger<CatchingEnvironmentController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ICatchingEnvironmentService catchingEnvironmentService)
            : base(logger, httpContextAccessor, mapper)
        {
            _catchingEnvironmentService = catchingEnvironmentService;
        }

        /// <summary>
        /// Create a new catching environment
        /// </summary>
        [HttpPost]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create Catching Environment", Description = "Create a new catching environment (Admin only)")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<CatchingEnvironmentResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        public async Task<IActionResult> CreateCatchingEnvironment([FromBody] CreateCatchingEnvironmentRequest request)
        {
            var result = await _catchingEnvironmentService.CreateCatchingEnvironmentAsync(request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Catching environment created successfully!"));
        }

        /// <summary>
        /// Get catching environment by ID
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get Catching Environment by ID", Description = "Get detailed information of a catching environment")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<CatchingEnvironmentResponse>))]
        [SwaggerResponse(404, "Catching environment not found")]
        public async Task<IActionResult> GetCatchingEnvironmentById(int id)
        {
            var result = await _catchingEnvironmentService.GetCatchingEnvironmentByIdAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Get all catching environments
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get All Catching Environments", Description = "Get all catching environments without pagination")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<CatchingEnvironmentResponse>>))]
        public async Task<IActionResult> GetAllCatchingEnvironments()
        {
            var result = await _catchingEnvironmentService.GetAllCatchingEnvironmentsAsync();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        /// <summary>
        /// Update an existing catching environment
        /// </summary>
        [HttpPut("{id}")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update Catching Environment", Description = "Update an existing catching environment (Admin only)")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<CatchingEnvironmentResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Catching environment not found")]
        public async Task<IActionResult> UpdateCatchingEnvironment(int id, [FromBody] UpdateCatchingEnvironmentRequest request)
        {
            var result = await _catchingEnvironmentService.UpdateCatchingEnvironmentAsync(id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Catching environment updated successfully!"));
        }

        /// <summary>
        /// Delete a catching environment
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete Catching Environment", Description = "Delete a catching environment (Admin only)")]
        [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<bool>))]
        [SwaggerResponse(400, "Cannot delete if in use")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Catching environment not found")]
        public async Task<IActionResult> DeleteCatchingEnvironment(int id)
        {
            var result = await _catchingEnvironmentService.DeleteCatchingEnvironmentAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Catching environment deleted successfully!"));
        }
    }
}
