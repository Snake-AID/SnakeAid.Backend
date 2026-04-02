using SnakeAid.Core.Domains;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Notification;

namespace SnakeAid.Service.Interfaces;

public interface INotificationQueueService
{
    Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default);

    Task PublishBulkAsync(
        IEnumerable<NotificationMessage> messages,
        IEnumerable<AppNotification> appNotifications,
        CancellationToken cancellationToken = default);

    Task<int> BroadcastAsync(AdminBroadcastNotificationRequest request, CancellationToken cancellationToken = default);
}
