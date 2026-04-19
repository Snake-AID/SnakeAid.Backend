using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnakeAid.Api.Hubs;
using SnakeAid.Core.Constants;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Services
{
    /// <summary>
    /// SignalR implementation of IRescueNotificationService.
    /// Lives in API layer to keep Service layer clean from SignalR dependencies.
    /// </summary>
    public class SignalRRescueNotificationService : IRescueNotificationService
    {
        private readonly IHubContext<RescuerHub> _hubContext;
        private readonly INotificationQueueService _notificationQueueService;
        private readonly ILogger<SignalRRescueNotificationService> _logger;

        // Static dictionary để track connected rescuers: userId -> connectionId
        public static ConcurrentDictionary<string, string> ConnectedRescuers { get; } = new();

        public SignalRRescueNotificationService(
            IHubContext<RescuerHub> hubContext,
            INotificationQueueService notificationQueueService,
            ILogger<SignalRRescueNotificationService> logger)
        {
            _hubContext = hubContext;
            _notificationQueueService = notificationQueueService;
            _logger = logger;
        }

        public bool IsRescuerConnected(string rescuerId)
        {
            return ConnectedRescuers.ContainsKey(rescuerId);
        }

        public async Task NotifyDispatchRequestedAsync(string rescuerId, DispatchRequestNotificationPayload requestData)
        {
            await SafeExecuteAsync(async () =>
            {
                if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("DispatchRequested", requestData);
                }

                await _hubContext.Clients.Group("Monitors").SendAsync("DispatchRequested", new { RescuerId = rescuerId, Data = requestData });

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = Guid.Parse(rescuerId),
                    Title = "Điều phối viên gửi yêu cầu hỗ trợ khẩn cấp cho bạn",
                    Body = BuildRequestSummary(requestData) ?? "Điều phối viên vừa gửi yêu cầu cho bạn.",
                    Type = "SNAKE_RESCUE_DISPATCH_REQUESTED",
                    Data = BuildEntityData(requestData)
                });
            }, "NotifyDispatchRequested", rescuerId);
        }

        public async Task NotifyRescuerAcceptedAsync(string rescuerId, AcceptRescueResponse acceptedData)
            => await NotifyRescuerAndMonitorsAsync(rescuerId, "RequestAccepted",
                connId => _hubContext.Clients.Client(connId).SendAsync("RequestAccepted", acceptedData),
                new { RescuerId = rescuerId, Data = acceptedData });

        public async Task NotifyRescuerDeclinedAsync(string rescuerId, RejectRescueResponse declinedData)
            => await NotifyRescuerAndMonitorsAsync(rescuerId, "RequestDeclined",
                connId => _hubContext.Clients.Client(connId).SendAsync("RequestDeclined", declinedData),
                new { RescuerId = rescuerId, Data = declinedData });

        public async Task NotifyRequestCancelledAsync(string rescuerId, Guid requestId, string? cancelReason = null)
            => await SafeExecuteAsync(async () =>
            {
                var reasonCode = string.IsNullOrWhiteSpace(cancelReason)
                    ? DispatchRequestCancelReasonCodes.CancelledByMember
                    : cancelReason;
                var message = BuildCancelledMessage(reasonCode);

                if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("RequestCancelled", new { RequestId = requestId, ReasonCode = reasonCode, Message = message });
                }

                var payload = new RescuerRequestNotificationPayload
                {
                    RequestId = requestId,
                    RescuerId = Guid.Parse(rescuerId),
                    ReasonCode = reasonCode,
                    Message = message
                };
                await _hubContext.Clients.Group("Monitors").SendAsync("RequestCancelled", payload);
                await PublishRescuerNotificationAsync(rescuerId, "RequestCancelled", payload);
            }, "RequestCancelled", rescuerId);

        public async Task NotifyRequestExpiredAsync(string rescuerId, Guid requestId)
            => await SafeExecuteAsync(async () =>
            {
                if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("RequestExpired", new { RequestId = requestId, Message = "Yêu cầu đã hết hạn." });
                }

                var payload = new RescuerRequestNotificationPayload
                {
                    RequestId = requestId,
                    RescuerId = Guid.Parse(rescuerId),
                    Message = "Yêu cầu đã hết hạn."
                };
                await _hubContext.Clients.Group("Monitors").SendAsync("RequestExpired", payload);
                await PublishRescuerNotificationAsync(rescuerId, "RequestExpired", payload);
            }, "RequestExpired", rescuerId);

        /// <summary>
        /// Force disconnect a rescuer from RescuerHub.
        /// Sends a "ForceDisconnect" signal to client; client should disconnect gracefully.
        /// </summary>
        public async Task ForceDisconnectRescuerAsync(string rescuerId, string reason)
        {
            if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
            {
                await SafeExecuteAsync(async () =>
                {
                    // Send disconnect signal to client
                    await _hubContext.Clients.Client(connectionId).SendAsync("ForceDisconnect", new
                    {
                        Reason = reason,
                        Message = $"You have been disconnected from RescuerHub: {reason}",
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("Sent ForceDisconnect signal to rescuer {RescuerId} (ConnectionId: {ConnectionId}), reason: {Reason}",
                        rescuerId, connectionId, reason);
                }, "ForceDisconnect", rescuerId);

                // Notify monitors
                await _hubContext.Clients.Group("Monitors").SendAsync("AdminLog", new
                {
                    Type = "RescuerForceDisconnected",
                    UserId = rescuerId,
                    Reason = reason,
                    Message = $"Rescuer {rescuerId} force-disconnected: {reason}",
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Cannot force disconnect rescuer {RescuerId} - not connected", rescuerId);
            }
        }

        #region Helper methods
        private async Task NotifyRescuerAndMonitorsAsync(string rescuerId, string actionName, Func<string, Task> rescuerAction, object monitorData)
        {
            // Always try notify Rescuer if online
            if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
            {
                await SafeExecuteAsync(async () =>
                {
                    await rescuerAction(connectionId);
                }, actionName, rescuerId);
            }
            else
                _logger.LogWarning("Rescuer {RescuerId} not connected for {Action}", rescuerId, actionName);

            // Always notify Monitors
            await SafeExecuteAsync(() => _hubContext.Clients.Group("Monitors").SendAsync(actionName, monitorData), $"{actionName}Monitor", "Monitors");
        }

        private async Task SafeExecuteAsync(Func<Task> action, string actionName, string contextId)
        {
            try
            {
                await action();
                _logger.LogInformation("Successfully executed {Action} for {ContextId}", actionName, contextId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error in {actionName} for {contextId}", ex),
                    "SignalR_Notification_Error");
            }
        }

        private async Task PublishRescuerNotificationAsync(string rescuerId, string actionName, object payload)
        {
            if (!Guid.TryParse(rescuerId, out var userId))
            {
                return;
            }

            var title = actionName switch
            {
                "RequestCancelled" => "Nhiệm vụ bị hủy",
                "RequestExpired" => "Yêu cầu đã hết hạn",
                "NotifyDispatchRequested" => "Điều phối viên đang gửi yêu cầu",
                _ => "Thông báo mới"
            };

            await _notificationQueueService.PublishAsync(new NotificationMessage
            {
                UserId = userId,
                Title = title,
                Body = BuildRequestSummary(payload) ?? "Mở app để xem chi tiết.",
                Type = GetNotificationType(actionName),
                Data = BuildEntityData(payload)
            });
        }

        private static Dictionary<string, string>? BuildEntityData(object payload)
        {
            switch (payload)
            {
                case DispatchRequestNotificationPayload p:
                    return new Dictionary<string, string>
                    {
                        ["requestId"] = p.RequestId.ToString(),
                        ["incidentId"] = p.IncidentId.ToString(),
                        ["rescuerId"] = p.RescuerId.ToString()
                    };
                case RescuerRequestNotificationPayload p:
                    return new Dictionary<string, string>
                    {
                        ["requestId"] = p.RequestId.ToString(),
                        ["rescuerId"] = p.RescuerId.ToString()
                    };
                case AcceptRescueResponse p:
                    return new Dictionary<string, string>
                    {
                        ["requestId"] = p.RequestId.ToString(),
                        ["incidentId"] = p.IncidentId.ToString(),
                        ["missionId"] = p.MissionId.ToString(),
                        ["rescuerId"] = p.RescuerId.ToString()
                    };
                case RejectRescueResponse p:
                    return new Dictionary<string, string>
                    {
                        ["requestId"] = p.RequestId.ToString()
                    };
                default:
                    return null;
            }
        }

        private static string GetNotificationType(string actionName)
            => actionName switch
            {
                "RequestCancelled" => "SNAKE_RESCUE_REQUEST_CANCELLED_BY_MEMBER",
                "RequestExpired" => "SNAKE_RESCUE_REQUEST_EXPIRED",
                "NotifyDispatchRequested" => "SNAKE_RESCUE_DISPATCH_REQUESTED",
                _ => actionName
            };

        private static string? BuildRequestSummary(object payload)
            => payload switch
            {
                DispatchRequestNotificationPayload p => !string.IsNullOrWhiteSpace(p.Message)
                    ? p.Message
                    : $"Yêu cầu #{p.RequestId}.",
                RescuerRequestNotificationPayload p => p.Message,
                AcceptRescueResponse p => p.Message,
                RejectRescueResponse p => p.Message,
                _ => null
            };

        private static string BuildCancelledMessage(string reasonCode)
            => reasonCode switch
            {
                DispatchRequestCancelReasonCodes.CancelledByRedispatch
                    => "Yêu cầu đã bị hủy vì điều phối viên đã điều phối cho cứu hộ viên khác.",
                DispatchRequestCancelReasonCodes.CancelledByOperator
                    => "Yêu cầu đã bị hủy bởi điều phối viên.",
                _ => "Nhiệm vụ đã bị hủy bởi người dùng."
            };

        #endregion

        #region Static methods for Hub to manage connections

        public static void AddConnection(string userId, string connectionId)
        {
            var sizeBefore = ConnectedRescuers.Count;
            ConnectedRescuers[userId] = connectionId;
            var sizeAfter = ConnectedRescuers.Count;
            Console.WriteLine($"[SignalR] AddConnection: userId={userId}, connId={connectionId}, size: {sizeBefore}→{sizeAfter}");
        }

        public static void RemoveConnection(string userId)
        {
            var sizeBefore = ConnectedRescuers.Count;
            var removed = ConnectedRescuers.TryRemove(userId, out var removedConnId);
            var sizeAfter = ConnectedRescuers.Count;
            Console.WriteLine($"[SignalR] RemoveConnection: userId={userId}, removed={removed}, connId={removedConnId}, size: {sizeBefore}→{sizeAfter}");
        }

        public static string? GetConnectionId(string userId)
        {
            return ConnectedRescuers.TryGetValue(userId, out var connectionId) ? connectionId : null;
        }

        #endregion
    }
}
