using Mapster;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.AppNotification;
using SnakeAid.Core.Responses.AppNotification;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class AppNotificationService : IAppNotificationService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<AppNotificationService> _logger;

        public AppNotificationService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<AppNotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AppNotificationResponse> CreateAppNotificationAsync(Guid currentUserId, CreateAppNotificationRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var userExists = await _unitOfWork.GetRepository<Account>()
                    .ExistsAsync(a => a.Id == currentUserId);

                if (!userExists)
                {
                    throw new NotFoundException($"User with ID {currentUserId} not found.");
                }

                var appNotification = new AppNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUserId,
                    Title = request.Title,
                    Message = request.Message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<AppNotification>().InsertAsync(appNotification);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Created app notification with ID: {Id}", appNotification.Id);

                return appNotification.Adapt<AppNotificationResponse>();
            });
        }

        public async Task<AppNotificationResponse> GetAppNotificationByIdAsync(Guid currentUserId, Guid id)
        {
            var appNotification = await _unitOfWork.GetRepository<AppNotification>()
                .FirstOrDefaultAsync(predicate: n => n.Id == id && n.UserId == currentUserId);

            if (appNotification == null)
            {
                throw new NotFoundException($"App notification with ID {id} not found.");
            }

            return appNotification.Adapt<AppNotificationResponse>();
        }

        public async Task<List<AppNotificationResponse>> GetAllAppNotificationsAsync(Guid currentUserId)
        {
            var appNotifications = await _unitOfWork.GetRepository<AppNotification>()
                .GetListAsync(
                    predicate: n => n.UserId == currentUserId,
                    orderBy: query => query.OrderByDescending(n => n.CreatedAt));

            return appNotifications.Adapt<List<AppNotificationResponse>>();
        }

        public async Task<AppNotificationResponse> UpdateAppNotificationAsync(Guid currentUserId, Guid id, UpdateAppNotificationRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var appNotification = await _unitOfWork.GetRepository<AppNotification>()
                    .FirstOrDefaultAsync(predicate: n => n.Id == id && n.UserId == currentUserId);

                if (appNotification == null)
                {
                    throw new NotFoundException($"App notification with ID {id} not found.");
                }

                appNotification.IsRead = request.IsRead!.Value;

                appNotification.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.GetRepository<AppNotification>().Update(appNotification);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Updated app notification with ID: {Id}", id);

                return appNotification.Adapt<AppNotificationResponse>();
            });
        }

        public async Task<bool> DeleteAppNotificationAsync(Guid currentUserId, Guid id)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var appNotification = await _unitOfWork.GetRepository<AppNotification>()
                    .FirstOrDefaultAsync(predicate: n => n.Id == id && n.UserId == currentUserId);

                if (appNotification == null)
                {
                    throw new NotFoundException($"App notification with ID {id} not found.");
                }

                _unitOfWork.GetRepository<AppNotification>().Delete(appNotification);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Deleted app notification with ID: {Id}", id);

                return true;
            });
        }
    }
}