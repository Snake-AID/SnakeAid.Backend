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
                var newResponse = new
                {
                    response.Id,
                    response.UserId,
                    response.Address,
                    response.LocationCoordinates,
                    response.AdditionalDetails,
                    response.Status,
                    response.EstimatedPrice,
                    response.DistanceKm,
                    response.CreatedAt,
                    response.User
                };

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestCreated", newResponse);
                await _hubContext.Clients.User(newResponse.UserId.ToString()).SendAsync("SnakeCatchingRequestCreated", newResponse);
            }, "SnakeCatchingRequestCreated", response.Id);

        public Task NotifyRequestConfirmedAsync(CreateSnakeCatchingRequestResponse response)
            => SafeExecuteAsync(async () =>
            {
                var newResponse = new
                {
                    response.Id,
                    response.Status,
                    response.ConfirmedAt,
                    response.PrePaidAt,
                    response.IsPrePaid
                };

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestAccepted", newResponse);
                await _hubContext.Clients.User(response.UserId.ToString()).SendAsync("SnakeCatchingRequestAccepted", newResponse);
            }, "SnakeCatchingRequestAccepted", response.Id);

        public Task NotifyRequestAssignedAsync(CreateSnakeCatchingRequestResponse response)
            => SafeExecuteAsync(async () =>
            {
                var newResponse = new
                {
                    response.Id,
                    response.Status,
                    response.AssignedAt,
                    response.AssignedRescuerId,
                    response.AssignedRescuer
                };

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestAssigned", newResponse);
                await _hubContext.Clients.User(response.UserId.ToString()).SendAsync("SnakeCatchingRequestAssigned", newResponse);

                if (response.AssignedRescuerId.HasValue)
                {
                    await _hubContext.Clients.User(newResponse.AssignedRescuerId.Value.ToString()).SendAsync("SnakeCatchingRequestAssigned", newResponse);
                }
            }, "SnakeCatchingRequestAssigned", response.Id);

        public Task NotifyRequestCancelledAsync(DetailSnakeCatchingRequestResponse response)
            => SafeExecuteAsync(async () =>
            {
                var newResponse = new
                {
                    response.Id,
                    response.UserId,
                    response.Status,
                    response.CancellationReason
                    
                };

                await _hubContext.Clients.Group(OperatorGroup).SendAsync("SnakeCatchingRequestCancelled", newResponse);
                await _hubContext.Clients.User(response.UserId.ToString()).SendAsync("SnakeCatchingRequestCancelled", newResponse);

                if (response.AssignedRescuerId.HasValue)
                {
                    await _hubContext.Clients.User(response.AssignedRescuerId.Value.ToString()).SendAsync("SnakeCatchingRequestCancelled", newResponse);
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