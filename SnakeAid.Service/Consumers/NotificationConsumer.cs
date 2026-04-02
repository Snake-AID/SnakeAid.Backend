using MassTransit;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Consumers;

public class NotificationConsumer : IConsumer<NotificationMessage>
{
    private readonly IFirebaseNotificationService _firebaseNotificationService;
    private readonly ILogger<NotificationConsumer> _logger;

    public NotificationConsumer(
        IFirebaseNotificationService firebaseNotificationService,
        ILogger<NotificationConsumer> logger)
    {
        _firebaseNotificationService = firebaseNotificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<NotificationMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Consuming notification {NotificationId} for user {UserId} with type {Type}",
            message.NotificationId,
            message.UserId,
            message.Type);

        await _firebaseNotificationService.SendAsync(message, context.CancellationToken);
    }
}
