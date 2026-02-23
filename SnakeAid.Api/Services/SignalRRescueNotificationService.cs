using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnakeAid.Api.Hubs;
using SnakeAid.Service.Interfaces;

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
            if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
            {
                try
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("NewRescueRequest", requestData);
                    _logger.LogInformation("Sent rescue request to rescuer {RescuerId}", rescuerId);

                    // Optional: Broadcast to Monitors for dashboard tracking
                    await _hubContext.Clients.Group("Monitors").SendAsync("NewRescueRequest", new
                    {
                        RescuerId = rescuerId,
                        Data = requestData
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending request to rescuer {RescuerId}: {Message}", rescuerId, ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("Rescuer {RescuerId} not connected, cannot send request", rescuerId);
            }
        }

        public async Task NotifyRequestTakenAsync(string rescuerId, Guid requestId)
        {
            if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
            {
                try
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("RequestTaken", new
                    {
                        RequestId = requestId,
                        Message = "This request has been taken by another rescuer."
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying rescuer {RescuerId} about taken request: {Message}", rescuerId, ex.Message);
                }
            }

            // Always notify Monitors regardless of rescuer connection state
            await _hubContext.Clients.Group("Monitors").SendAsync("RequestTaken", new
            {
                RequestId = requestId,
                TargetRescuerId = rescuerId
            });
        }

        public async Task NotifyRequestCancelledAsync(string rescuerId, Guid requestId)
        {
            if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
            {
                try
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("RequestCancelled", new
                    {
                        RequestId = requestId,
                        Message = "This request has been cancelled by the user."
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying rescuer {RescuerId} about cancelled request: {Message}", rescuerId, ex.Message);
                }
            }

            // Always notify Monitors regardless of rescuer connection state
            await _hubContext.Clients.Group("Monitors").SendAsync("RequestCancelled", new
            {
                RequestId = requestId,
                TargetRescuerId = rescuerId
            });
        }

        public async Task NotifyRequestExpiredAsync(string rescuerId, Guid requestId)
        {
            if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
            {
                try
                {
                    _logger.LogInformation("Sending RequestExpired notification to rescuer {RescuerId} (connectionId: {ConnectionId})",
                        rescuerId, connectionId);

                    await _hubContext.Clients.Client(connectionId).SendAsync("RequestExpired", new
                    {
                        RequestId = requestId,
                        Message = "This request has expired."
                    });

                    _logger.LogInformation("Successfully sent RequestExpired notification to rescuer {RescuerId}", rescuerId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying rescuer {RescuerId} about expired request: {Message}", rescuerId, ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("Cannot send RequestExpired to rescuer {RescuerId} - not in ConnectedRescuers dictionary. Current connections: {Count}",
                    rescuerId, ConnectedRescuers.Count);
            }

            // Always notify Monitors regardless of rescuer connection state
            await _hubContext.Clients.Group("Monitors").SendAsync("RequestExpired", new
            {
                RequestId = requestId,
                TargetRescuerId = rescuerId
            });
        }

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
