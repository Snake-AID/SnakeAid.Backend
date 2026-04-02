using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[Route("api/admin/notifications")]
[ApiController]
// [Authorize(Roles = "Admin")]
public class AdminNotificationController : BaseController<AdminNotificationController>
{
    private readonly INotificationQueueService _notificationQueueService;

    public AdminNotificationController(
        ILogger<AdminNotificationController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        INotificationQueueService notificationQueueService)
        : base(logger, httpContextAccessor, mapper)
    {
        _notificationQueueService = notificationQueueService;
    }

    [HttpPost("push")]
    [SwaggerOperation(
        Summary = "Queue push notification",
        Description = "Admin queues a push notification to be consumed asynchronously by RabbitMQ consumer")]
    public async Task<IActionResult> QueuePushNotification(
        [FromBody] AdminPushNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var notification = new NotificationMessage
        {
            UserId = request.UserId,
            Title = request.Title,
            Body = request.Body,
            Type = request.Type,
            Data = request.Data
        };

        await _notificationQueueService.PublishAsync(notification, cancellationToken);

        return Ok(ApiResponseBuilder.BuildSuccessResponse(new
        {
            notificationId = notification.NotificationId,
            queuedAtUtc = notification.CreatedAtUtc
        }, "Notification queued successfully"));
    }

    [HttpPost("broadcast")]
    [SwaggerOperation(
        Summary = "Broadcast push notification",
        Description = "Admin queues push notifications to all active users and stores in-app notifications in DB")]
    public async Task<IActionResult> BroadcastNotification(
        [FromBody] AdminBroadcastNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var recipientCount = await _notificationQueueService.BroadcastAsync(request, cancellationToken);

        if (recipientCount == 0)
        {
            return NotFound(ApiResponseBuilder.BuildErrorResponse("No active users found for broadcast."));
        }

        return Ok(ApiResponseBuilder.BuildSuccessResponse(new
        {
            recipients = recipientCount,
            queuedAtUtc = DateTime.UtcNow
        }, "Broadcast notification queued successfully"));
    }
}

