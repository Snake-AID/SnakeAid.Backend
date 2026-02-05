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
        }

        public async Task NotifyRequestExpiredAsync(string rescuerId, Guid requestId)
        {
            if (ConnectedRescuers.TryGetValue(rescuerId, out var connectionId))
            {
                try
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("RequestExpired", new
                    {
                        RequestId = requestId,
                        Message = "This request has expired."
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying rescuer {RescuerId} about expired request: {Message}", rescuerId, ex.Message);
                }
            }
        }

        #region Static methods for Hub to manage connections

        public static void AddConnection(string userId, string connectionId)
        {
            ConnectedRescuers[userId] = connectionId;
        }

        public static void RemoveConnection(string userId)
        {
            ConnectedRescuers.TryRemove(userId, out _);
        }

        public static string? GetConnectionId(string userId)
        {
            return ConnectedRescuers.TryGetValue(userId, out var connectionId) ? connectionId : null;
        }

        #endregion
    }
}
