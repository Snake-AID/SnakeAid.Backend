using SnakeAid.Core.Requests.AppNotification;
using SnakeAid.Core.Responses.AppNotification;

namespace SnakeAid.Service.Interfaces
{
    public interface IAppNotificationService
    {
        Task<AppNotificationResponse> CreateAppNotificationAsync(Guid currentUserId, CreateAppNotificationRequest request);

        Task<AppNotificationResponse> GetAppNotificationByIdAsync(Guid currentUserId, Guid id);

        Task<List<AppNotificationResponse>> GetAllAppNotificationsAsync(Guid currentUserId);

        Task<AppNotificationResponse> UpdateAppNotificationAsync(Guid currentUserId, Guid id, UpdateAppNotificationRequest request);

        Task<bool> DeleteAppNotificationAsync(Guid currentUserId, Guid id);
    }
}