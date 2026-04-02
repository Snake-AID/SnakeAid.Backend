using SnakeAid.Core.Messages.Notifications;

namespace SnakeAid.Service.Interfaces;

public interface INotificationQueueService
{
    Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
