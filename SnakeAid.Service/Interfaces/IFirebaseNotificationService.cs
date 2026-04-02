using SnakeAid.Core.Messages.Notifications;

namespace SnakeAid.Service.Interfaces;

public interface IFirebaseNotificationService
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
