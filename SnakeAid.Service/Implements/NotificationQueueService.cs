using MassTransit;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class NotificationQueueService : INotificationQueueService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<NotificationQueueService> _logger;

    public NotificationQueueService(
        IPublishEndpoint publishEndpoint,
        ILogger<NotificationQueueService> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (message.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required", nameof(message));
        }

        await _publishEndpoint.Publish(message, cancellationToken);

        _logger.LogInformation(
            "Queued notification {NotificationId} for user {UserId} with type {Type}",
            message.NotificationId,
            message.UserId,
            message.Type);
    }
}
