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
    }
}