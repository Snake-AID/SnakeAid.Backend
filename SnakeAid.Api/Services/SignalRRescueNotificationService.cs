using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnakeAid.Api.Hubs;
using SnakeAid.Service.Interfaces;
using SnakeAid.Core.Exceptions;

namespace SnakeAid.Api.Services
{
    /// <summary>
    /// SignalR implementation of IRescueNotificationService.
    /// Lives in API layer to keep Service layer clean from SignalR dependencies.
    /// </summary>
    public class SignalRRescueNotificationService : IRescueNotificationService
    {
        private readonly IHubContext<RescuerHub> _hubContext;
        private readonly ILogger<SignalRRescueNotificationService> _logger;

        // Static dictionary để track connected rescuers: userId -> connectionId
        public static ConcurrentDictionary<string, string> ConnectedRescuers { get; } = new();

        public SignalRRescueNotificationService(
            IHubContext<RescuerHub> hubContext,
            ILogger<SignalRRescueNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public bool IsRescuerConnected(string rescuerId)
        {
            return ConnectedRescuers.ContainsKey(rescuerId);
        }

        public async Task SendNewRequestAsync(string rescuerId, object requestData)
        {
            // Business logic: only notify monitors if request is actually sent to a connected rescuer
            if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
                await SafeExecuteAsync(async () =>
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("NewRescueRequest", requestData);
                    await _hubContext.Clients.Group("Monitors").SendAsync("NewRescueRequest", new { RescuerId = rescuerId, Data = requestData });
                }, "SendNewRequest", rescuerId);
            else
                _logger.LogWarning("Rescuer {RescuerId} not connected, cannot send request", rescuerId);
        }

        public async Task NotifyRequestTakenAsync(string rescuerId, Guid requestId)
            => await NotifyRescuerAndMonitorsAsync(rescuerId, "RequestTaken",
                connId => _hubContext.Clients.Client(connId).SendAsync("RequestTaken", new { RequestId = requestId, Message = "This request has been taken by another rescuer." }),
                new { RequestId = requestId, TargetRescuerId = rescuerId });

        public async Task NotifyRequestCancelledAsync(string rescuerId, Guid requestId)
            => await NotifyRescuerAndMonitorsAsync(rescuerId, "RequestCancelled",
                connId => _hubContext.Clients.Client(connId).SendAsync("RequestCancelled", new { RequestId = requestId, Message = "This request has been cancelled by the user." }),
                new { RequestId = requestId, TargetRescuerId = rescuerId });

        public async Task NotifyRequestExpiredAsync(string rescuerId, Guid requestId)
            => await NotifyRescuerAndMonitorsAsync(rescuerId, "RequestExpired",
                connId => _hubContext.Clients.Client(connId).SendAsync("RequestExpired", new { RequestId = requestId, Message = "This request has expired." }),
                new { RequestId = requestId, TargetRescuerId = rescuerId });

        #region Helper methods
        private async Task NotifyRescuerAndMonitorsAsync(string rescuerId, string actionName, Func<string, Task> rescuerAction, object monitorData)
        {
            // Always try notify Rescuer if online
            if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
                await SafeExecuteAsync(() => rescuerAction(connectionId), actionName, rescuerId);
            else
                _logger.LogWarning("Rescuer {RescuerId} not connected for {Action}", rescuerId, actionName);

            // Always notify Monitors
            await SafeExecuteAsync(() => _hubContext.Clients.Group("Monitors").SendAsync(actionName, monitorData), $"{actionName}Monitor", "Monitors");
        }

        private async Task SafeExecuteAsync(Func<Task> action, string actionName, string contextId)
        {
            try
            {
                await action();
                _logger.LogInformation("Successfully executed {Action} for {ContextId}", actionName, contextId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(new SignalRNotificationException($"Error in {actionName} for {contextId}", ex),
                    "SignalR_Notification_Error");
            }
        }
        #endregion

        #region Static methods for Hub to manage connections

        public static void AddConnection(string userId, string connectionId)
        {
            var sizeBefore = ConnectedRescuers.Count;
            ConnectedRescuers[userId] = connectionId;
            var sizeAfter = ConnectedRescuers.Count;
            Console.WriteLine($"[SignalR] AddConnection: userId={userId}, connId={connectionId}, size: {sizeBefore}→{sizeAfter}");
        }

        public static void RemoveConnection(string userId)
        {
            var sizeBefore = ConnectedRescuers.Count;
            var removed = ConnectedRescuers.TryRemove(userId, out var removedConnId);
            var sizeAfter = ConnectedRescuers.Count;
            Console.WriteLine($"[SignalR] RemoveConnection: userId={userId}, removed={removed}, connId={removedConnId}, size: {sizeBefore}→{sizeAfter}");
        }

        public static string? GetConnectionId(string userId)
        {
            return ConnectedRescuers.TryGetValue(userId, out var connectionId) ? connectionId : null;
        }

        #endregion
    }
}
