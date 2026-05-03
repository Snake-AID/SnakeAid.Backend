using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnakeAid.Api.Hubs;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Api.Services
{
    public class SignalRExpertEmergencyNotificationService : IExpertEmergencyNotificationService
    {
        private readonly IHubContext<ExpertHub> _hubContext;
        private readonly INotificationQueueService _notificationQueueService;
        private readonly ILogger<SignalRExpertEmergencyNotificationService> _logger;

        public static ConcurrentDictionary<string, string> ConnectedExperts { get; } = new();

        public SignalRExpertEmergencyNotificationService(
            IHubContext<ExpertHub> hubContext,
            INotificationQueueService notificationQueueService,
            ILogger<SignalRExpertEmergencyNotificationService> logger)
        {
            _hubContext = hubContext;
            _notificationQueueService = notificationQueueService;
            _logger = logger;
        }

        public bool IsExpertConnected(string expertId)
        {
            return ConnectedExperts.ContainsKey(expertId);
        }

        public async Task SendEmergencyRequestAsync(string expertId, object requestData)
        {
            if (!ConnectedExperts.TryGetValue(expertId, out var connectionId))
            {
                _logger.LogWarning("Expert {ExpertId} is not connected. Skip emergency request push.", expertId);
                return;
            }

            await SafeExecuteAsync(
                () => _hubContext.Clients.Client(connectionId).SendAsync("EmergencyConsultationRequest", requestData),
                "SendEmergencyRequest",
                expertId);
        }

        public async Task NotifyEmergencyRequestStatusChangedAsync(Guid requestId, object statusData)
        {
            var groupName = BuildEmergencyRequestGroupName(requestId);
            await SafeExecuteAsync(
                () => _hubContext.Clients.Group(groupName).SendAsync("EmergencyRequestStatusChanged", statusData),
                "NotifyEmergencyRequestStatusChanged",
                groupName);
        }

        public async Task NotifyEmergencyRequestCreatedAsync(Guid requestId, Guid memberId, Guid expertId)
        {
            await SafeTryPublishNotificationAsync(
                new NotificationMessage
                {
                    UserId = memberId,
                    Title = "Yêu cầu tư vấn khẩn cấp đã được gửi",
                    Body = "Yêu cầu tư vấn khẩn cấp của bạn đang được gửi đến chuyên gia.",
                    Type = "EMERGENCY_CONSULTATION_REQUEST_CREATED",
                    Data = new Dictionary<string, string>
                    {
                        ["requestId"] = requestId.ToString(),
                        ["expertId"] = expertId.ToString()
                    }
                },
                "emergency consultation request created notification",
                memberId);
        }

        public async Task NotifyEmergencyRequestAcceptedAsync(Guid requestId, Guid expertId)
        {
            await SafeTryPublishNotificationAsync(
                new NotificationMessage
                {
                    UserId = expertId,
                    Title = "Cuộc tư vấn khẩn cấp được chấp nhận",
                    Body = "Bạn đã chấp nhận một yêu cầu tư vấn khẩn cấp. Vui lòng sẵn sàng để bắt đầu cuộc gọi.",
                    Type = "EMERGENCY_CONSULTATION_REQUEST_ACCEPTED",
                    Data = new Dictionary<string, string>
                    {
                        ["requestId"] = requestId.ToString()
                    }
                },
                "emergency consultation request accepted notification",
                expertId);
        }

        public async Task NotifyEmergencyRequestRejectedAsync(Guid requestId, Guid expertId)
        {
            await SafeTryPublishNotificationAsync(
                new NotificationMessage
                {
                    UserId = expertId,
                    Title = "Yêu cầu tư vấn khẩn cấp bị từ chối",
                    Body = "Bạn đã từ chối một yêu cầu tư vấn khẩn cấp.",
                    Type = "EMERGENCY_CONSULTATION_REQUEST_REJECTED",
                    Data = new Dictionary<string, string>
                    {
                        ["requestId"] = requestId.ToString()
                    }
                },
                "emergency consultation request rejected notification",
                expertId);
        }

        public static void AddConnection(string expertId, string connectionId)
        {
            ConnectedExperts[expertId] = connectionId;
        }

        public static bool RemoveConnection(string expertId)
        {
            return ConnectedExperts.TryRemove(expertId, out _);
        }

        public static string? FindExpertIdByConnection(string connectionId)
        {
            var item = ConnectedExperts.FirstOrDefault(x => x.Value == connectionId);
            return item.Equals(default(KeyValuePair<string, string>)) ? null : item.Key;
        }

        public static string BuildEmergencyRequestGroupName(Guid requestId)
        {
            return $"consultation:emergency:request:{requestId:N}";
        }

        private async Task SafeExecuteAsync(Func<Task> action, string actionName, string expertId)
        {
            try
            {
                await action();
                _logger.LogInformation("Executed {Action} for expert {ExpertId}", actionName, expertId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error in {actionName} for expert {expertId}", ex), "SignalR_Notification_Error");
            }
        }

        private async Task SafeTryPublishNotificationAsync(
            NotificationMessage message,
            string actionName,
            Guid userId)
        {
            try
            {
                await _notificationQueueService.PublishAsync(message);
                _logger.LogInformation("Published {Action} for userId {UserId}", actionName, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish {Action} for userId {UserId}", actionName, userId);
            }
        }
    }
}
