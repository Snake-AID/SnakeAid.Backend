using Microsoft.AspNetCore.SignalR;
using SnakeAid.Api.Hubs;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Services
{
    public class SignalRSnakeCatchingRequestNotificationService : ISnakeCatchingRequestNotificationService
    {
        private const string OperatorGroup = "Operators";
        private readonly IHubContext<RescuerHub> _hubContext;
        private readonly INotificationQueueService _notificationQueueService;
        private readonly ILogger<SignalRSnakeCatchingRequestNotificationService> _logger;
        private static string FormatRequestCode(Guid requestId)
        {
            var shortId = requestId.ToString("N").Substring(26, 6).ToUpper(); 
            return $"CAR-{shortId}";
        }

        public SignalRSnakeCatchingRequestNotificationService(
            IHubContext<RescuerHub> hubContext,
            INotificationQueueService notificationQueueService,
            ILogger<SignalRSnakeCatchingRequestNotificationService> logger)
        {
            _hubContext = hubContext;
            _notificationQueueService = notificationQueueService;
            _logger = logger;
        }

        public Task NotifyRequestCreatedAsync(
            Guid requestId,
            Guid userId,
            string? address,
            double lat,
            double lng,
            string? additionalDetails,
            RequestStatus status,
            decimal? estimatedPrice,
            double? distanceKm,
            DateTime? createdAt,
            string? userName,
            string? userPhone)
            => SafeExecuteAsync(async () =>
            {
                var safeLat = NormalizeDouble(lat);
                var safeLng = NormalizeDouble(lng);
                var safeDistance = NormalizeDouble(distanceKm);

                var newResponse = new
                {
                    Id = requestId,
                    UserId = userId,
                    Address = address,
                    Lat = safeLat,
                    Lng = safeLng,
                    AdditionalDetails = additionalDetails,
                    Status = status,
                    EstimatedPrice = estimatedPrice,
                    DistanceKm = safeDistance,
                    CreatedAt = createdAt,
                    User = new
                    {
                        UserName = userName,
                        PhoneNumber = userPhone
                    }
                };

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestCreated", newResponse);
                await _hubContext.Clients.User(userId.ToString()).SendAsync("SnakeCatchingRequestCreated", newResponse);

                await _notificationQueueService.BroadcastAsync(new AdminBroadcastNotificationRequest
                {
                    Title = "Có đơn bắt rắn mới",
                    Body = $"Có yêu cầu bắt rắn mới từ {userName ?? "thành viên"} - {address ?? "chưa rõ địa chỉ"}.",
                    Type = "SNAKE_CATCHING_REQUEST_CREATED",
                    TargetRoles = new List<AccountRole> { AccountRole.Operator },
                    Data = BuildEntityData(requestId)
                });
            }, "SnakeCatchingRequestCreated", requestId);

        public Task NotifyRequestConfirmedAsync(
            Guid requestId,
            Guid userId,
            RequestStatus status,
            DateTime? confirmedAt,
            DateTime? prePaidAt,
            bool isPrePaid)
            => SafeExecuteAsync(async () =>
            {
                var newResponse = new
                {
                    Id = requestId,
                    Status = status,
                    ConfirmedAt = confirmedAt,
                    PrePaidAt = prePaidAt,
                    IsPrePaid = isPrePaid
                };

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestAccepted", newResponse);
                await _hubContext.Clients.User(userId.ToString()).SendAsync("SnakeCatchingRequestAccepted", newResponse);
                var requestCode = FormatRequestCode(requestId);
                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = userId,
                    Title = "Yêu cầu bắt rắn đã được xác nhận",
                    Body = $"Yêu cầu bắt rắn #{requestCode} đã được xác nhận.",
                    Type = "SNAKE_CATCHING_REQUEST_CONFIRMED",
                    Data = BuildEntityData(requestId)
                });
            }, "SnakeCatchingRequestAccepted", requestId);

        public Task NotifyRequestAssignedAsync(
            Guid requestId,
            Guid userId,
            RequestStatus status,
            DateTime? assignedAt,
            Guid? assignedRescuerId,
            string? assignedRescuerName,
            string? assignedRescuerPhone)
            => SafeExecuteAsync(async () =>
            {
                var newResponse = new
                {
                    Id = requestId,
                    Status = status,
                    AssignedAt = assignedAt,
                    AssignedRescuerId = assignedRescuerId,
                    AssignedRescuerName = assignedRescuerName,
                    AssignedRescuerPhone = assignedRescuerPhone
                };

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestAssigned", newResponse);
                await _hubContext.Clients.User(userId.ToString()).SendAsync("SnakeCatchingRequestAssigned", newResponse);
                var requestCode = FormatRequestCode(requestId);
                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = userId,
                    Title = "Cứu hộ viên đang được phân công",
                    Body = $"{assignedRescuerName ?? "Cứu hộ viên"} sẽ đến hỗ trợ bạn.",
                    Type = "SNAKE_CATCHING_RESCUER_ASSIGNED",
                    Data = BuildEntityData(requestId)
                });

                if (assignedRescuerId.HasValue)
                {
                    await _hubContext.Clients.User(assignedRescuerId.Value.ToString()).SendAsync("SnakeCatchingRequestAssigned", newResponse);

                    await _notificationQueueService.PublishAsync(new NotificationMessage
                    {
                        UserId = assignedRescuerId.Value,
                        Title = "Bạn được phân công nhiệm vụ bắt rắn mới",
                        Body = $"Mã nhiệm vụ: #{requestCode}. Mở app để xem chi tiết.",
                        Type = "SNAKE_CATCHING_MISSION_ASSIGNED",
                        Data = BuildEntityData(requestId)
                    });
                }
            }, "SnakeCatchingRequestAssigned", requestId);

        public Task NotifyRequestCancelledAsync(
            Guid requestId,
            Guid userId,
            RequestStatus status,
            string? cancellationReason,
            Guid? assignedRescuerId)
            => SafeExecuteAsync(async () =>
            {
                var newResponse = new
                {
                    Id = requestId,
                    UserId = userId,
                    Status = status,
                    CancellationReason = cancellationReason
                };

                var requestCode = FormatRequestCode(requestId);

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestCancelled", newResponse);
                await _hubContext.Clients.User(userId.ToString()).SendAsync("SnakeCatchingRequestCancelled", newResponse);

                // Send push notification to member
                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = userId,
                    Title = "Yêu cầu đã hủy",
                    Body = $"Yêu cầu #{requestCode} đã được hủy.",
                    Type = "SNAKE_CATCHING_REQUEST_CANCELLED_BY_MEMBER",
                    Data = BuildEntityData(requestId)
                });

                if (assignedRescuerId.HasValue)
                {
                    await _hubContext.Clients.User(assignedRescuerId.Value.ToString()).SendAsync("SnakeCatchingRequestCancelled", newResponse);

                    await _notificationQueueService.PublishAsync(new NotificationMessage
                    {
                        UserId = assignedRescuerId.Value,
                        Title = "Nhiệm vụ bị hủy",
                        Body = $"Khách hàng đã hủy yêu cầu #{requestCode}.",
                        Type = "SNAKE_CATCHING_REQUEST_CANCELLED_BY_MEMBER",
                        Data = BuildEntityData(requestId)
                    });
                }
            }, "SnakeCatchingRequestCancelled", requestId);

        public Task NotifyOperatorCancelledAsync(
            Guid requestId,
            Guid userId,
            RequestStatus status,
            string? cancellationReason,
            Guid? assignedRescuerId,
            decimal? depositAmount)
            => SafeExecuteAsync(async () =>
            {
                var newResponse = new
                {
                    Id = requestId,
                    UserId = userId,
                    Status = status,
                    CancellationReason = cancellationReason
                };

                var requestCode = FormatRequestCode(requestId);

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestCancelled", newResponse);
                await _hubContext.Clients.User(userId.ToString()).SendAsync("SnakeCatchingRequestCancelled", newResponse);

                // Send push notification to customer with different message based on deposit amount
                var customerTitle = depositAmount.HasValue ? "Yêu cầu bị hủy - Hoàn tiền" : "Yêu cầu bị hủy";
                var customerBody = depositAmount.HasValue
                    ? $"Yêu cầu #{requestCode} đã bị hủy bởi điều phối viên. Tiền phí di chuyển {depositAmount.Value:N0} VND sẽ được hoàn lại."
                    : $"Yêu cầu #{requestCode} đã bị hủy bởi điều phối viên.";

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = userId,
                    Title = customerTitle,
                    Body = customerBody,
                    Type = "SNAKE_CATCHING_REQUEST_CANCELLED_BY_OPERATOR",
                    Data = BuildEntityData(requestId)
                });

                if (assignedRescuerId.HasValue)
                {
                    await _hubContext.Clients.User(assignedRescuerId.Value.ToString()).SendAsync("SnakeCatchingRequestCancelled", newResponse);

                    await _notificationQueueService.PublishAsync(new NotificationMessage
                    {
                        UserId = assignedRescuerId.Value,
                        Title = "Nhiệm vụ bị hủy",
                        Body = $"Yêu cầu #{requestCode} đã bị hủy bởi điều phối viên.",
                        Type = "SNAKE_CATCHING_REQUEST_CANCELLED_BY_OPERATOR",
                        Data = BuildEntityData(requestId)
                    });
                }
            }, "SnakeCatchingRequestCancelledByOperator", requestId);

        public Task NotifyMissionEnRouteAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName,
            int? estimatedMinutes = null)
            => SafeExecuteAsync(async () =>
            {
                var requestCode = FormatRequestCode(requestId);

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Cứu hộ viên đang di chuyển đến",
                    Body = $"{rescuerName ?? "Cứu hộ viên"} đang di chuyển đến bạn{(estimatedMinutes.HasValue ? $". Dự kiến {estimatedMinutes.Value} phút." : ".")}",
                    Type = "SNAKE_CATCHING_RESCUER_EN_ROUTE",
                    Data = BuildEntityData(requestId, missionId)
                });
            }, "SnakeCatchingRescuerEnRoute", requestId);

        public Task NotifyMissionArrivedAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName)
            => SafeExecuteAsync(async () =>
            {
                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Cứu hộ viên đã đến",
                    Body = $"{rescuerName ?? "Cứu hộ viên"} đã đến nơi và bắt đầu khảo sát.",
                    Type = "SNAKE_CATCHING_RESCUER_ARRIVED",
                    Data = BuildEntityData(requestId, missionId)
                });
            }, "SnakeCatchingRescuerArrived", requestId);

        public async Task NotifyMissionCompletedAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName,
            decimal? actualCost)
        {
            await SafeExecuteAsync(async () =>
            {
                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Nhiệm vụ hoàn thành - Cần thanh toán",
                    Body = actualCost.HasValue
                        ? $"{rescuerName ?? "Cứu hộ viên"} đã hoàn thành. Phí dịch vụ: {actualCost.Value:N0} VND."
                        : $"{rescuerName ?? "Cứu hộ viên"} đã hoàn thành nhiệm vụ. Vui lòng thanh toán để kết thúc.",
                    Type = "SNAKE_CATCHING_MISSION_COMPLETED",
                    Data = BuildEntityData(requestId, missionId)
                });
            }, "SnakeCatchingMissionCompletedQueue", requestId);

            await SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingMissionCompleted", new
                {
                    RequestId = requestId,
                    MissionId = missionId,
                    MemberUserId = memberUserId,
                    RescuerUserId = rescuerUserId,
                    RescuerName = rescuerName,
                    ActualCost = actualCost,
                    CompletedAt = DateTime.UtcNow
                });
            }, "SnakeCatchingMissionCompleted", requestId);
        }

        public Task NotifyMissionUncompletedAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName,
            string reason)
            => SafeExecuteAsync(async () =>
            {
                var payload = new
                {
                    RequestId = requestId,
                    MissionId = missionId,
                    RescuerUserId = rescuerUserId,
                    RescuerName = rescuerName,
                    Reason = reason
                };

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingMissionUncompleted", payload);
                await _hubContext.Clients.User(memberUserId.ToString()).SendAsync("SnakeCatchingMissionUncompleted", payload);
                await _hubContext.Clients.User(rescuerUserId.ToString()).SendAsync("SnakeCatchingMissionUncompleted", payload);

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Nhiệm vụ chưa hoàn thành",
                    Body = $"{rescuerName ?? "Cứu hộ viên"} báo cáo chưa thể hoàn thành nhiệm vụ. Lý do: {reason}",
                    Type = "SNAKE_CATCHING_MISSION_UNCOMPLETED",
                    Data = BuildEntityData(requestId, missionId)
                });

                await _notificationQueueService.BroadcastAsync(new AdminBroadcastNotificationRequest
                {
                    Title = "Nhiệm vụ bắt rắn chưa hoàn thành",
                    Body = $"Nhiệm vụ #{missionId} của đơn #{requestId} được đánh dấu chưa hoàn thành. Lý do: {reason}",
                    Type = "SNAKE_CATCHING_MISSION_UNCOMPLETED",
                    TargetRoles = new List<AccountRole> { AccountRole.Operator },
                    Data = BuildEntityData(requestId, missionId)
                });
            }, "SnakeCatchingMissionUncompleted", requestId);

        public Task NotifyMissionAbortedAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            Guid? operatorUserId,
            string? rescuerName,
            string? reason)
            => SafeExecuteAsync(async () =>
            {
                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Cứu hộ viên không thể thực hiện",
                    Body = $"{rescuerName ?? "Cứu hộ viên"} không thể tiếp tục nhiệm vụ. Vui lòng đợi điều phối viên đang tìm người thay thế.",
                    Type = "SNAKE_CATCHING_MISSION_ABORTED",
                    Data = BuildEntityData(requestId, missionId)
                });
                var requestCode = FormatRequestCode(requestId);
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingMissionAborted", new
                {
                    RequestId = requestId,
                    MissionId = missionId,
                    RescuerId = rescuerUserId,
                    OperatorUserId = operatorUserId,
                    RescuerName = rescuerName,
                    Reason = reason,
                    UpdatedAt = DateTime.UtcNow
                });

                await _notificationQueueService.BroadcastAsync(new AdminBroadcastNotificationRequest
                {
                    Title = "Cần phân công lại ngay",
                    Body = $"Cứu hộ viên {rescuerName ?? rescuerUserId.ToString()} đã hủy nhiệm vụ cho đơn #{requestCode}.",
                    Type = "SNAKE_CATCHING_REASSIGN_NEEDED",
                    TargetRoles = new List<AccountRole> { AccountRole.Operator },
                    Data = BuildEntityData(requestId, missionId)
                });
            }, "SnakeCatchingMissionAborted", requestId);

        private static Dictionary<string, string> BuildEntityData(Guid requestId, Guid? missionId = null)
        {
            var data = new Dictionary<string, string>
            {
                ["requestId"] = requestId.ToString()
            };

            if (missionId.HasValue)
            {
                data["missionId"] = missionId.Value.ToString();
            }

            return data;
        }

        private static double? NormalizeDouble(double? value)
        {
            if (!value.HasValue)
                return null;

            return double.IsFinite(value.Value) ? value : null;
        }

        private async Task SafeExecuteAsync(Func<Task> action, string actionName, Guid requestId)
        {
            try
            {
                await action();
                _logger.LogInformation("Broadcasted {ActionName} for snake catching request {RequestId}", actionName, requestId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error notifying {actionName} for snake catching request {requestId}", ex),
                    "SignalR_SnakeCatchingRequest_Notification_Error");
            }
        }
    }
}
