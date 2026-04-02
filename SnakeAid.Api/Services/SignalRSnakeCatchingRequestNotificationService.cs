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
                    Title = "Don bat ran moi",
                    Body = $"Co yeu cau bat ran moi tu {userName ?? "thanh vien"} - {address ?? "chua ro dia chi"}.",
                    Type = "SNAKE_CATCHING_REQUEST_CREATED",
                    TargetRoles = new List<AccountRole> { AccountRole.Operator },
                    Data = new Dictionary<string, string>
                    {
                        ["notificationType"] = "SNAKE_CATCHING_REQUEST_CREATED",
                        ["requestId"] = requestId.ToString()
                    }
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

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = userId,
                    Title = "Yeu cau da duoc xac nhan",
                    Body = $"Yeu cau #{requestId} da duoc xac nhan.",
                    Type = "SNAKE_CATCHING_REQUEST_CONFIRMED",
                    Data = new Dictionary<string, string>
                    {
                        ["notificationType"] = "SNAKE_CATCHING_REQUEST_CONFIRMED",
                        ["requestId"] = requestId.ToString()
                    }
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

                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = userId,
                    Title = "Cuu ho vien da duoc phan cong",
                    Body = $"{assignedRescuerName ?? "Cuu ho vien"} se den ho tro ban.",
                    Type = "SNAKE_CATCHING_RESCUER_ASSIGNED",
                    Data = new Dictionary<string, string>
                    {
                        ["notificationType"] = "SNAKE_CATCHING_RESCUER_ASSIGNED",
                        ["requestId"] = requestId.ToString()
                    }
                });

                if (assignedRescuerId.HasValue)
                {
                    await _hubContext.Clients.User(assignedRescuerId.Value.ToString()).SendAsync("SnakeCatchingRequestAssigned", newResponse);

                    await _notificationQueueService.PublishAsync(new NotificationMessage
                    {
                        UserId = assignedRescuerId.Value,
                        Title = "Ban duoc giao nhiem vu bat ran",
                        Body = $"Nhiem vu moi cho don #{requestId}. Mo app de xem chi tiet.",
                        Type = "SNAKE_CATCHING_MISSION_ASSIGNED",
                        Data = new Dictionary<string, string>
                        {
                            ["notificationType"] = "SNAKE_CATCHING_MISSION_ASSIGNED",
                            ["requestId"] = requestId.ToString()
                        }
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

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestCancelled", newResponse);
                await _hubContext.Clients.User(userId.ToString()).SendAsync("SnakeCatchingRequestCancelled", newResponse);

                if (assignedRescuerId.HasValue)
                {
                    await _hubContext.Clients.User(assignedRescuerId.Value.ToString()).SendAsync("SnakeCatchingRequestCancelled", newResponse);

                    await _notificationQueueService.PublishAsync(new NotificationMessage
                    {
                        UserId = assignedRescuerId.Value,
                        Title = "Nhiem vu bi huy",
                        Body = $"Khach hang da huy yeu cau #{requestId}.",
                        Type = "SNAKE_CATCHING_REQUEST_CANCELLED_BY_MEMBER",
                        Data = new Dictionary<string, string>
                        {
                            ["notificationType"] = "SNAKE_CATCHING_REQUEST_CANCELLED_BY_MEMBER",
                            ["requestId"] = requestId.ToString()
                        }
                    });
                }
            }, "SnakeCatchingRequestCancelled", requestId);

        public Task NotifyMissionEnRouteAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName,
            int? estimatedMinutes = null)
            => SafeExecuteAsync(async () =>
            {
                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Cuu ho vien dang di chuyen",
                    Body = $"{rescuerName ?? "Cuu ho vien"} dang tren duong den ban{(estimatedMinutes.HasValue ? $". Du kien {estimatedMinutes.Value} phut." : ".")}",
                    Type = "SNAKE_CATCHING_RESCUER_EN_ROUTE",
                    Data = new Dictionary<string, string>
                    {
                        ["notificationType"] = "SNAKE_CATCHING_RESCUER_EN_ROUTE",
                        ["requestId"] = requestId.ToString(),
                        ["missionId"] = missionId.ToString()
                    }
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
                    Title = "Cuu ho vien da den",
                    Body = $"{rescuerName ?? "Cuu ho vien"} da den noi va bat dau khao sat.",
                    Type = "SNAKE_CATCHING_RESCUER_ARRIVED",
                    Data = new Dictionary<string, string>
                    {
                        ["notificationType"] = "SNAKE_CATCHING_RESCUER_ARRIVED",
                        ["requestId"] = requestId.ToString(),
                        ["missionId"] = missionId.ToString()
                    }
                });
            }, "SnakeCatchingRescuerArrived", requestId);

        public Task NotifyMissionCompletedAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName,
            decimal? actualCost)
            => SafeExecuteAsync(async () =>
            {
                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Nhiem vu hoan thanh - Can thanh toan",
                    Body = actualCost.HasValue
                        ? $"{rescuerName ?? "Cuu ho vien"} da hoan thanh. Phi dich vu: {actualCost.Value:N0} VND."
                        : $"{rescuerName ?? "Cuu ho vien"} da hoan thanh nhiem vu. Vui long thanh toan de ket thuc.",
                    Type = "SNAKE_CATCHING_MISSION_COMPLETED",
                    Data = new Dictionary<string, string>
                    {
                        ["notificationType"] = "SNAKE_CATCHING_MISSION_COMPLETED",
                        ["requestId"] = requestId.ToString(),
                        ["missionId"] = missionId.ToString()
                    }
                });
            }, "SnakeCatchingMissionCompleted", requestId);

        public Task NotifyMissionAbortedAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName,
            string? reason)
            => SafeExecuteAsync(async () =>
            {
                await _notificationQueueService.PublishAsync(new NotificationMessage
                {
                    UserId = memberUserId,
                    Title = "Cuu ho vien khong the thuc hien",
                    Body = $"{rescuerName ?? "Cuu ho vien"} khong the tiep tuc nhiem vu. Doi SnakeAid dang tim nguoi thay the.",
                    Type = "SNAKE_CATCHING_MISSION_ABORTED",
                    Data = new Dictionary<string, string>
                    {
                        ["notificationType"] = "SNAKE_CATCHING_MISSION_ABORTED",
                        ["requestId"] = requestId.ToString(),
                        ["missionId"] = missionId.ToString()
                    }
                });

                await _notificationQueueService.BroadcastAsync(new AdminBroadcastNotificationRequest
                {
                    Title = "Can phan cong lai ngay",
                    Body = $"Rescuer {rescuerName ?? rescuerUserId.ToString()} da huy nhiem vu cho don #{requestId}.",
                    Type = "SNAKE_CATCHING_REASSIGN_NEEDED",
                    TargetRoles = new List<AccountRole> { AccountRole.Operator },
                    Data = new Dictionary<string, string>
                    {
                        ["notificationType"] = "SNAKE_CATCHING_REASSIGN_NEEDED",
                        ["requestId"] = requestId.ToString(),
                        ["missionId"] = missionId.ToString(),
                        ["reason"] = reason ?? string.Empty
                    }
                });
            }, "SnakeCatchingMissionAborted", requestId);

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