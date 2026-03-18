using Microsoft.AspNetCore.SignalR;
using SnakeAid.Api.Hubs;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Services
{
    public class SignalRSnakeCatchingRequestNotificationService : ISnakeCatchingRequestNotificationService
    {
        private const string OperatorGroup = "Operators";
        private readonly IHubContext<RescuerHub> _hubContext;
        private readonly ILogger<SignalRSnakeCatchingRequestNotificationService> _logger;

        public SignalRSnakeCatchingRequestNotificationService(
            IHubContext<RescuerHub> hubContext,
            ILogger<SignalRSnakeCatchingRequestNotificationService> logger)
        {
            _hubContext = hubContext;
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

                if (assignedRescuerId.HasValue)
                {
                    await _hubContext.Clients.User(assignedRescuerId.Value.ToString()).SendAsync("SnakeCatchingRequestAssigned", newResponse);
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
                }
            }, "SnakeCatchingRequestCancelled", requestId);

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