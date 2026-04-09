using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnakeAid.Api.Hubs;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Services
{
    public class SignalRMissionNotificationService : IMissionNotificationService
    {
        private readonly IHubContext<MissionHub> _hubContext;
        private readonly INotificationQueueService _notificationQueueService;
        private readonly ILogger<SignalRMissionNotificationService> _logger;

        public SignalRMissionNotificationService(
            IHubContext<MissionHub> hubContext,
            INotificationQueueService notificationQueueService,
            ILogger<SignalRMissionNotificationService> logger)
        {
            _hubContext = hubContext;
            _notificationQueueService = notificationQueueService;
            _logger = logger;
        }

        public async Task NotifyRescuerAcceptedAsync(Guid incidentId, Guid memberUserId, AcceptRescueResponse rescuerInfo)
            => await SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(incidentId.ToString())
                    .SendAsync("RescuerAccepted", rescuerInfo);

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Cứu hộ viên đã nhận yêu cầu",
                    Body = "Cứu hộ viên đã xác nhận yêu cầu của bạn và đang chuẩn bị di chuyển.",
                    Type = "SNAKE_RESCUE_REQUEST_ACCEPTED",
                    Data = BuildEntityData(incidentId)
                });
            }, "RescuerAccepted", incidentId);

        public async Task NotifyMissionStartedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, MissionStartedNotificationPayload missionInfo)
            => await SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionStarted", new
                {
                    status = missionInfo.Status
                });

                var rescuerName = missionInfo.RescuerName;
                var estimatedMinutes = missionInfo.EstimatedMinutes;

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Cứu hộ viên đang di chuyển",
                    Body = !string.IsNullOrWhiteSpace(rescuerName)
                        ? $"{rescuerName} đang trên đường đến vị trí của bạn."
                        : "Cứu hộ viên đang trên đường đến vị trí của bạn.",
                    Type = "SNAKE_RESCUE_RESCUER_EN_ROUTE",
                    Data = BuildEntityData(incidentId)
                });
            }, "MissionStarted", incidentId);

        public async Task NotifyRescuerArrivedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, string? rescuerName = null)
            => await SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("RescuerArrived");

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Cứu hộ viên đã đến",
                    Body = !string.IsNullOrWhiteSpace(rescuerName)
                        ? $"{rescuerName} đã đến nơi và bắt đầu khảo sát hiện trường."
                        : "Cứu hộ viên đã đến nơi và bắt đầu khảo sát hiện trường.",
                    Type = "SNAKE_RESCUE_RESCUER_ARRIVED",
                    Data = BuildEntityData(incidentId)
                });
            }, "RescuerArrived", incidentId);

        public async Task NotifyMissionCompletedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, MissionCompletedNotificationPayload result)
            => await SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionCompleted", new
                {
                    missionId = result.MissionId
                });

                var rescuerName = result.RescuerName;
                var actualCost = result.ActualCost;

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Nhiệm vụ hoàn thành - Cần thanh toán",
                    Body = actualCost.HasValue
                        ? (!string.IsNullOrWhiteSpace(rescuerName)
                            ? $"{rescuerName} đã hoàn thành nhiệm vụ. Phí dịch vụ: {actualCost.Value:N0} VNĐ."
                            : $"Cứu hộ viên đã hoàn thành nhiệm vụ. Phí dịch vụ: {actualCost.Value:N0} VNĐ.")
                        : (!string.IsNullOrWhiteSpace(rescuerName)
                            ? $"{rescuerName} đã hoàn thành nhiệm vụ. Vui lòng thanh toán để kết thúc dịch vụ."
                            : "Cứu hộ viên đã hoàn thành nhiệm vụ. Vui lòng thanh toán để kết thúc dịch vụ."),
                    Type = "SNAKE_RESCUE_MISSION_COMPLETED",
                    Data = BuildEntityData(incidentId, result.MissionId)
                });
            }, "MissionCompleted", incidentId);

        public async Task NotifyMissionCancelledAsync(Guid incidentId, Guid rescuerUserId, string reason)
            => await SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionCancelled", new { Reason = reason });

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = rescuerUserId,
                    Title = "Nhiệm vụ bị hủy",
                    Body = string.IsNullOrWhiteSpace(reason)
                        ? "Khách hàng đã hủy yêu cầu. Nhiệm vụ của bạn được dừng lại."
                        : $"Khách hàng đã hủy yêu cầu. Lý do: {reason}",
                    Type = "SNAKE_RESCUE_REQUEST_CANCELLED_BY_MEMBER",
                    Data = BuildEntityData(incidentId)
                });
            }, "MissionCancelled", incidentId);

        public async Task NotifyMissionAbortedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, string? rescuerName, string reason)
            => await SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(incidentId.ToString()).SendAsync("MissionAborted", new { Reason = reason });

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Cứu hộ viên không thể thực hiện",
                    Body = !string.IsNullOrWhiteSpace(rescuerName)
                        ? $"{rescuerName} không thể hoàn thành nhiệm vụ. Đội SnakeAid đang tìm cứu hộ viên thay thế cho bạn."
                        : "Cứu hộ viên không thể hoàn thành nhiệm vụ. Đội SnakeAid đang tìm người thay thế.",
                    Type = "SNAKE_RESCUE_MISSION_ABORTED",
                    Data = BuildEntityData(incidentId)
                });
            }, "MissionAborted", incidentId);

        private static Dictionary<string, string> BuildEntityData(Guid incidentId, Guid? missionId = null)
        {
            var data = new Dictionary<string, string>
            {
                ["incidentId"] = incidentId.ToString()
            };

            if (missionId.HasValue)
            {
                data["missionId"] = missionId.Value.ToString();
            }

            return data;
        }

        private async Task SafeExecuteAsync(Func<Task> action, string actionName, Guid incidentId)
        {
            try
            {
                await action();
                _logger.LogInformation("Notified {Action} for incident {IncidentId}", actionName, incidentId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying {actionName} for incident {incidentId}", ex),
                    "SignalR_Notification_Error");
            }
        }

    }
}
