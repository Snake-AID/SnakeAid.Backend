using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.Notification;

namespace SnakeAid.Service.Interfaces;

public interface IUserNotificationService
{
    Task<PagedData<AppNotificationResponse>> GetMyNotificationsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<AppNotificationResponse> MarkAsReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default);
}