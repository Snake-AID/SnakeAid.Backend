using System.Linq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class NotificationQueueService : INotificationQueueService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<NotificationQueueService> _logger;
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

    public NotificationQueueService(
        IPublishEndpoint publishEndpoint,
        ILogger<NotificationQueueService> logger,
        IUnitOfWork<SnakeAidDbContext> unitOfWork)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (message.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required", nameof(message));
        }

        var payloadJson = message.Data == null ? null : JsonSerializer.Serialize(message.Data);
        var deepLink = message.DeepLink ?? BuildDeepLink(message.Type, message.Data);

        await _unitOfWork.GetRepository<AppNotification>().InsertAsync(new AppNotification
        {
            Id = Guid.NewGuid(),
            UserId = message.UserId,
            Title = message.Title,
            Message = message.Body,
            NotificationType = message.Type,
            DeepLink = deepLink,
            PayloadJson = payloadJson,
            IsRead = false
        }, cancellationToken);
        await _unitOfWork.CommitAsync();

        message.DeepLink = deepLink;

        await _publishEndpoint.Publish(message, cancellationToken);

        _logger.LogInformation(
            "Queued notification {NotificationId} for user {UserId} with type {Type}",
            message.NotificationId,
            message.UserId,
            message.Type);
    }

    public async Task PublishBulkAsync(
        IEnumerable<NotificationMessage> messages,
        IEnumerable<AppNotification> appNotifications,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages?.ToList() ?? new List<NotificationMessage>();
        var appNotificationList = appNotifications?.ToList() ?? new List<AppNotification>();

        if (!messageList.Any())
        {
            throw new ArgumentException("At least one notification message is required.", nameof(messages));
        }

        if (!appNotificationList.Any())
        {
            throw new ArgumentException("At least one app notification is required.", nameof(appNotifications));
        }

        var appNotificationRepository = _unitOfWork.GetRepository<AppNotification>();
        await appNotificationRepository.InsertRangeAsync(appNotificationList, cancellationToken);
        await _unitOfWork.CommitAsync();

        const int publishBatchSize = 100;
        for (var index = 0; index < messageList.Count; index += publishBatchSize)
        {
            var batch = messageList.Skip(index).Take(publishBatchSize).ToList();
            await Task.WhenAll(batch.Select(message => _publishEndpoint.Publish(message, cancellationToken)));
        }

        _logger.LogInformation(
            "Queued bulk notification set with {Count} entries",
            messageList.Count);
    }

    public async Task<int> BroadcastAsync(AdminBroadcastNotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ArgumentException("Title and Body are required for broadcast.");
        }

        var accountRepository = _unitOfWork.GetRepository<Account>();
        var recipientsQuery = accountRepository.CreateBaseQuery();

        recipientsQuery = recipientsQuery.Where(user => user.IsActive);

        if (request.TargetRoles is { Count: > 0 })
        {
            recipientsQuery = recipientsQuery.Where(user => request.TargetRoles.Contains(user.Role));
        }

        var recipientIds = await recipientsQuery
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        if (recipientIds.Count == 0)
        {
            _logger.LogWarning("Broadcast notification aborted: no active users found.");
            return 0;
        }

        var notificationId = Guid.NewGuid();
        var appNotifications = recipientIds.Select(userId => new AppNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Message = request.Body,
            NotificationType = request.Type,
            DeepLink = BuildDeepLink(request.Type, request.Data),
            PayloadJson = request.Data == null ? null : JsonSerializer.Serialize(request.Data),
            IsRead = false
        }).ToList();

        var deepLink = BuildDeepLink(request.Type, request.Data);
        var messages = recipientIds.Select(userId => new NotificationMessage
        {
            NotificationId = notificationId,
            UserId = userId,
            Title = request.Title,
            Body = request.Body,
            Type = request.Type,
            DeepLink = deepLink,
            Data = request.Data
        }).ToList();

        await PublishBulkAsync(messages, appNotifications, cancellationToken);

        return recipientIds.Count;
    }

    private static string? BuildDeepLink(string notificationType, IReadOnlyDictionary<string, string>? data)
    {
        if (string.IsNullOrWhiteSpace(notificationType) || data == null)
        {
            return null;
        }

        return notificationType switch
        {
            "SNAKE_CATCHING_REQUEST_CREATED" => data.TryGetValue("requestId", out var requestIdCreated)
                ? $"/operator/requests/{requestIdCreated}"
                : null,
            "SNAKE_CATCHING_DEPOSIT_SUCCESS" => data.TryGetValue("requestId", out var requestIdDeposit)
                ? $"/snake-catching/detail/{requestIdDeposit}"
                : null,
            "SNAKE_CATCHING_REQUEST_EXPIRED" => "/snake-catching/history",
            "SNAKE_CATCHING_REQUEST_CONFIRMED" => data.TryGetValue("requestId", out var requestIdConfirmed)
                ? $"/snake-catching/detail/{requestIdConfirmed}"
                : null,
            "SNAKE_CATCHING_RESCUER_ASSIGNED" => data.TryGetValue("requestId", out var requestIdAssigned)
                ? $"/snake-catching/tracking/{requestIdAssigned}"
                : null,
            "SNAKE_CATCHING_MISSION_ASSIGNED" => data.TryGetValue("missionId", out var missionIdAssigned)
                ? $"/rescuer/mission/{missionIdAssigned}"
                : null,
            "SNAKE_CATCHING_RESCUER_EN_ROUTE" => data.TryGetValue("requestId", out var requestIdEnRoute)
                ? $"/snake-catching/tracking/{requestIdEnRoute}"
                : null,
            "SNAKE_CATCHING_RESCUER_ARRIVED" => data.TryGetValue("requestId", out var requestIdArrived)
                ? $"/snake-catching/tracking/{requestIdArrived}"
                : null,
            "SNAKE_CATCHING_MISSION_COMPLETED" => data.TryGetValue("requestId", out var requestIdCompleted)
                ? $"/snake-catching/payment/{requestIdCompleted}"
                : null,
            "SNAKE_CATCHING_PAYMENT_SUCCESS" => data.TryGetValue("requestId", out var requestIdPaymentSuccess)
                ? $"/snake-catching/detail/{requestIdPaymentSuccess}"
                : null,
            "SNAKE_CATCHING_PAYMENT_CONFIRMED" => data.TryGetValue("missionId", out var missionIdPaymentConfirmed)
                ? $"/rescuer/history/{missionIdPaymentConfirmed}"
                : null,
            "SNAKE_CATCHING_PAYMENT_FAILED" => data.TryGetValue("requestId", out var requestIdPaymentFailed)
                ? $"/snake-catching/payment/{requestIdPaymentFailed}"
                : null,
            "SNAKE_CATCHING_REQUEST_CANCELLED_BY_MEMBER" => data.TryGetValue("missionId", out var missionIdCancelled)
                ? $"/rescuer/history/{missionIdCancelled}"
                : null,
            "SNAKE_CATCHING_MISSION_ABORTED" => data.TryGetValue("requestId", out var requestIdAborted)
                ? $"/snake-catching/detail/{requestIdAborted}"
                : null,
            "SNAKE_CATCHING_REASSIGN_NEEDED" => data.TryGetValue("requestId", out var requestIdReassign)
                ? $"/operator/requests/{requestIdReassign}"
                : null,
            "WITHDRAWAL_REQUEST_CREATED" => data.TryGetValue("withdrawalId", out var createdWithdrawalId)
                ? $"/admin/withdrawals/{createdWithdrawalId}"
                : "/admin/withdrawals",
            "WITHDRAWAL_CANCELLED" => data.TryGetValue("withdrawalId", out var cancelledWithdrawalId)
                ? $"/admin/withdrawals/{cancelledWithdrawalId}"
                : "/admin/withdrawals",
            "WITHDRAWAL_APPROVED" => data.TryGetValue("withdrawalId", out var approvedWithdrawalId)
                ? $"/wallet/withdrawals/{approvedWithdrawalId}"
                : "/wallet/withdrawals",
            "WITHDRAWAL_REJECTED" => data.TryGetValue("withdrawalId", out var rejectedWithdrawalId)
                ? $"/wallet/withdrawals/{rejectedWithdrawalId}"
                : "/wallet/withdrawals",
            "WITHDRAWAL_COMPLETED" => data.TryGetValue("withdrawalId", out var completedWithdrawalId)
                ? $"/wallet/withdrawals/{completedWithdrawalId}"
                : "/wallet/withdrawals",
            "WITHDRAWAL_FAILED" => data.TryGetValue("withdrawalId", out var failedWithdrawalId)
                ? $"/wallet/withdrawals/{failedWithdrawalId}"
                : "/wallet/withdrawals",
            _ => data.TryGetValue("deepLink", out var explicitDeepLink) ? explicitDeepLink : null
        };
    }
}
