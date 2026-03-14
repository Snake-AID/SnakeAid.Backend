using SnakeAid.Core.Responses.SnakeCatchingRequest;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakeCatchingRequestNotificationService
    {
        Task NotifyRequestCreatedAsync(CreateSnakeCatchingRequestResponse response);

        Task NotifyRequestAcceptedAsync(CreateSnakeCatchingRequestResponse response);

        Task NotifyRequestAssignedAsync(CreateSnakeCatchingRequestResponse response);

        Task NotifyRequestCancelledAsync(DetailSnakeCatchingRequestResponse response);
    }
}