using Microsoft.AspNetCore.SignalR;
using SnakeAid.Api.Hubs;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Responses.SnakeCatchingRequest;
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

        public Task NotifyRequestCreatedAsync(CreateSnakeCatchingRequestResponse response)
            => SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestCreated", response);
                await _hubContext.Clients.User(response.UserId.ToString()).SendAsync("SnakeCatchingRequestCreated", response);
            }, "SnakeCatchingRequestCreated", response.Id);

        public Task NotifyRequestAcceptedAsync(CreateSnakeCatchingRequestResponse response)
            => SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestAccepted", response);
                await _hubContext.Clients.User(response.UserId.ToString()).SendAsync("SnakeCatchingRequestAccepted", response);
            }, "SnakeCatchingRequestAccepted", response.Id);

        public Task NotifyRequestAssignedAsync(CreateSnakeCatchingRequestResponse response)
            => SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestAssigned", response);
                await _hubContext.Clients.User(response.UserId.ToString()).SendAsync("SnakeCatchingRequestAssigned", response);

                if (response.AssignedRescuerId.HasValue)
                {
                    await _hubContext.Clients.User(response.AssignedRescuerId.Value.ToString()).SendAsync("SnakeCatchingRequestAssigned", response);
                }
            }, "SnakeCatchingRequestAssigned", response.Id);

        public Task NotifyRequestCancelledAsync(DetailSnakeCatchingRequestResponse response)
            => SafeExecuteAsync(async () =>
            {
                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestCancelled", response);
                await _hubContext.Clients.User(response.UserId.ToString()).SendAsync("SnakeCatchingRequestCancelled", response);

                if (response.AssignedRescuerId.HasValue)
                {
                    await _hubContext.Clients.User(response.AssignedRescuerId.Value.ToString()).SendAsync("SnakeCatchingRequestCancelled", response);
                }
            }, "SnakeCatchingRequestCancelled", response.Id);

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