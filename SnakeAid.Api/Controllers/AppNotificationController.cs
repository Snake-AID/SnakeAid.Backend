using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.AppNotification;
using SnakeAid.Core.Responses.AppNotification;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/app-notifications")]
    [ApiController]
    [Authorize]
    public class AppNotificationController : BaseController<AppNotificationController>
    {
        private readonly IAppNotificationService _appNotificationService;

        public AppNotificationController(
            ILogger<AppNotificationController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IAppNotificationService appNotificationService)
            : base(logger, httpContextAccessor, mapper)
        {
            _appNotificationService = appNotificationService;
        }

        [HttpPost]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create App Notification", Description = "Create a new app notification")]
        [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<AppNotificationResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(404, "User not found")]
        public async Task<IActionResult> CreateAppNotification([FromBody] CreateAppNotificationRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appNotificationService.CreateAppNotificationAsync(currentUserId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "App notification created successfully!"));
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get App Notification by ID", Description = "Get detailed information of an app notification")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<AppNotificationResponse>))]
        [SwaggerResponse(404, "App notification not found")]
        public async Task<IActionResult> GetAppNotificationById(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appNotificationService.GetAppNotificationByIdAsync(currentUserId, id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get All App Notifications", Description = "Get all app notifications")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<AppNotificationResponse>>))]
        public async Task<IActionResult> GetAllAppNotifications()
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appNotificationService.GetAllAppNotificationsAsync(currentUserId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpPut("{id}")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update App Notification", Description = "Update an existing app notification")]
        [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<AppNotificationResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(404, "App notification or user not found")]
        public async Task<IActionResult> UpdateAppNotification(Guid id, [FromBody] UpdateAppNotificationRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appNotificationService.UpdateAppNotificationAsync(currentUserId, id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "App notification updated successfully!"));
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete App Notification", Description = "Delete an app notification")]
        [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<bool>))]
        [SwaggerResponse(404, "App notification not found")]
        public async Task<IActionResult> DeleteAppNotification(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appNotificationService.DeleteAppNotificationAsync(currentUserId, id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "App notification deleted successfully!"));
        }
    }
}