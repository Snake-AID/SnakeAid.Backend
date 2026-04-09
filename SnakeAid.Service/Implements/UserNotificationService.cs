using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.Notification;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class UserNotificationService : IUserNotificationService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

    public UserNotificationService(IUnitOfWork<SnakeAidDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedData<AppNotificationResponse>> GetMyNotificationsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new BadRequestException("User id is required.");
        }

        if (page < 1)
        {
            throw new BadRequestException("Page must be greater than 0.");
        }

        if (pageSize < 1)
        {
            throw new BadRequestException("Page size must be greater than 0.");
        }

        var pagedNotifications = await _unitOfWork.GetRepository<AppNotification>()
            .GetPagingListAsync(
                selector: notification => notification,
                predicate: notification => notification.UserId == userId,
                orderBy: query => query.OrderByDescending(notification => notification.CreatedAt),
                page: page,
                size: pageSize,
                cancellationToken: cancellationToken);

        return new PagedData<AppNotificationResponse>
        {
            Items = pagedNotifications.Items.Select(ToResponse).ToList(),
            Meta = pagedNotifications.Meta
        };
    }

    public async Task<AppNotificationResponse> MarkAsReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new BadRequestException("User id is required.");
        }

        if (notificationId == Guid.Empty)
        {
            throw new BadRequestException("Notification id is required.");
        }

        var repository = _unitOfWork.GetRepository<AppNotification>();
        var notification = await repository.FirstOrDefaultAsync(
            predicate: item => item.Id == notificationId && item.UserId == userId,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (notification == null)
        {
            throw new NotFoundException("Notification not found.");
        }

        notification.IsRead = true;

        repository.Update(notification);
        await _unitOfWork.CommitAsync();

        return ToResponse(notification);
    }

    private static AppNotificationResponse ToResponse(AppNotification notification)
    {
        return new AppNotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            NotificationType = notification.NotificationType,
            DeepLink = notification.DeepLink,
            Data = ParsePayloadData(notification.PayloadJson),
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        };
    }

    private static Dictionary<string, string>? ParsePayloadData(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(payloadJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}