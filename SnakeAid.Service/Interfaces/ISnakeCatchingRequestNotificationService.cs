using SnakeAid.Core.Domains;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakeCatchingRequestNotificationService
    {
        Task NotifyRequestCreatedAsync(
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
            string? userPhone);

        Task NotifyRequestConfirmedAsync(
            Guid requestId,
            Guid userId,
            RequestStatus status,
            DateTime? confirmedAt,
            DateTime? prePaidAt,
            bool isPrePaid);

        Task NotifyRequestAssignedAsync(
            Guid requestId,
            Guid userId,
            RequestStatus status,
            DateTime? assignedAt,
            Guid? assignedRescuerId,
            string? assignedRescuerName,
            string? assignedRescuerPhone);

        Task NotifyRequestCancelledAsync(
            Guid requestId,
            Guid userId,
            RequestStatus status,
            string? cancellationReason,
            Guid? assignedRescuerId);

        Task NotifyMissionEnRouteAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName,
            int? estimatedMinutes = null);

        Task NotifyMissionArrivedAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName);

        Task NotifyMissionCompletedAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName,
            decimal? actualCost);

        Task NotifyMissionUncompletedAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName,
            string reason);

        Task NotifyMissionAbortedAsync(
            Guid requestId,
            Guid missionId,
            Guid memberUserId,
            Guid rescuerUserId,
            string? rescuerName,
            string? reason);
    }
}