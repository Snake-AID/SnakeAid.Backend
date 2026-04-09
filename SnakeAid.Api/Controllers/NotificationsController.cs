using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.Notification;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : BaseController<NotificationsController>
{
    private readonly IUserNotificationService _notificationService;

    public NotificationsController(
        ILogger<NotificationsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IUserNotificationService notificationService)
        : base(logger, httpContextAccessor, mapper)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get my notifications",
        Description = "Get paged in-app notifications for the current authenticated user")]
    [SwaggerResponse(200, "Notifications retrieved successfully", typeof(ApiResponse<PagedData<AppNotificationResponse>>))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetMyNotificationsAsync(userId, page, pageSize, cancellationToken);

        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Notifications retrieved successfully."));
    }

    [HttpPut("{notificationId:guid}/read")]
    [SwaggerOperation(
        Summary = "Mark notification as read",
        Description = "Mark a notification as read for the current authenticated user")]
    [SwaggerResponse(200, "Notification marked as read", typeof(ApiResponse<AppNotificationResponse>))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Notification not found")]
    public async Task<IActionResult> MarkAsRead(
        [FromRoute] Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.MarkAsReadAsync(userId, notificationId, cancellationToken);

        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Notification marked as read."));
    }
}