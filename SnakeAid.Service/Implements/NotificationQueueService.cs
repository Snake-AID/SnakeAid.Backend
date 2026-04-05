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
    private static readonly TimeSpan BrokerPublishTimeout = TimeSpan.FromSeconds(3);
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

        await _unitOfWork.GetRepository<AppNotification>().InsertAsync(new AppNotification
        {
            Id = Guid.NewGuid(),
            UserId = message.UserId,
            Title = message.Title,
            Message = message.Body,
            NotificationType = message.Type,
            DeepLink = null,
            PayloadJson = payloadJson,
            IsRead = false
        }, cancellationToken);
        await _unitOfWork.CommitAsync();

        await PublishToBrokerAsync(message, cancellationToken);

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
            await Task.WhenAll(batch.Select(message => PublishToBrokerAsync(message, cancellationToken)));
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
            DeepLink = null,
            PayloadJson = request.Data == null ? null : JsonSerializer.Serialize(request.Data),
            IsRead = false
        }).ToList();

        var messages = recipientIds.Select(userId => new NotificationMessage
        {
            NotificationId = notificationId,
            UserId = userId,
            Title = request.Title,
            Body = request.Body,
            Type = request.Type,
            DeepLink = null,
            Data = request.Data
        }).ToList();

        await PublishBulkAsync(messages, appNotifications, cancellationToken);

        return recipientIds.Count;
    }

    private async Task PublishToBrokerAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var publishTask = _publishEndpoint.Publish(message, cancellationToken);
            await publishTask.WaitAsync(BrokerPublishTimeout, cancellationToken);
        }
        catch (Exception ex) when (IsBrokerDeliveryFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Skipping broker publish for notification {NotificationType} and user {UserId}. App notification was already stored.",
                message.Type,
                message.UserId);
        }
    }

    private static bool IsBrokerDeliveryFailure(Exception exception)
    {
        return exception is TimeoutException
            || FindExceptionByTypeName(exception, "RabbitMQ.Client.Exceptions.BrokerUnreachableException") != null
            || FindExceptionByTypeName(exception, "RabbitMQ.Client.Exceptions.ConnectFailureException") != null
            || FindExceptionByTypeName(exception, "MassTransit.RabbitMqTransport.RabbitMqConnectionException") != null;
    }
    private static Exception? FindExceptionByTypeName(Exception exception, string fullTypeName)
    {
        Exception? current = exception;
        while (current != null)
        {
            if (string.Equals(current.GetType().FullName, fullTypeName, StringComparison.Ordinal))
            {
                return current;
            }

            current = current.InnerException;
        }

        return null;
    }
}

