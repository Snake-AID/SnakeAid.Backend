using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Api.Services;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Hubs
{
    public class RescuerHub : Hub
    {
        private readonly IRescueRequestSessionService _sessionService;
        private readonly ILogger<RescuerHub> _logger;

        // Static dictionary để track connected rescuers: userId -> connectionId
        public static ConcurrentDictionary<string, string> ConnectedRescuers => SignalRRescueNotificationService.ConnectedRescuers;

        public RescuerHub(
            IRescueRequestSessionService sessionService,
            ILogger<RescuerHub> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        /// <summary>
        /// Khi rescuer connect và join để nhận requests
        /// </summary>
        public async Task JoinAsRescuer(string userId)
        {
            // Add connection to notification service
            SignalRRescueNotificationService.AddConnection(userId, Context.ConnectionId);

            _logger.LogInformation("Rescuer {UserId} joined with connectionId {ConnectionId}", userId, Context.ConnectionId);
            await Clients.Caller.SendAsync("Joined", new
            {
                UserId = userId,
                ConnectionId = Context.ConnectionId,
                Message = $"Rescuer {userId} joined successfully. Waiting for rescue requests..."
            });
        }

        /// Rescuer accept request - Ai accept nhanh nhất sẽ nhận mission
        public async Task AcceptRequest(Guid requestId, Guid rescuerId)
        {
            try
            {
                await _sessionService.AcceptRequestAsync(requestId, rescuerId);

                await Clients.Caller.SendAsync("RequestAccepted", new
                {
                    RequestId = requestId,
                    Message = "Request accepted successfully! You have been assigned to this rescue mission."
                });

                _logger.LogInformation("Rescuer {RescuerId} accepted request {RequestId}", rescuerId, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting request {RequestId}: {Message}", requestId, ex.Message);
                await Clients.Caller.SendAsync("RequestError", new
                {
                    RequestId = requestId,
                    Error = ex.Message
                });
            }
        }

        /// Rescuer reject request
        public async Task RejectRequest(Guid requestId)
        {
            try
            {
                await _sessionService.RejectRequestAsync(requestId);

                await Clients.Caller.SendAsync("RequestRejected", new
                {
                    RequestId = requestId,
                    Message = "Request rejected."
                });

                _logger.LogInformation("Request {RequestId} rejected", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request {RequestId}: {Message}", requestId, ex.Message);
                await Clients.Caller.SendAsync("RequestError", new
                {
                    RequestId = requestId,
                    Error = ex.Message
                });
            }
        }

        public async Task UpdateLocation(string userId, double latitude, double longitude)
        {
            _logger.LogInformation("Rescuer {UserId} updated location: {Lat}, {Lng}", userId, latitude, longitude);
            await Clients.Caller.SendAsync("LocationUpdated", new
            {
                UserId = userId,
                Latitude = latitude,
                Longitude = longitude,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = ConnectedRescuers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (userId != null)
            {
                SignalRRescueNotificationService.RemoveConnection(userId);
                _logger.LogInformation("Rescuer {UserId} disconnected", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task GetConnectedRescuers()
        {
            var rescuers = ConnectedRescuers.Keys.ToList();
            await Clients.Caller.SendAsync("ConnectedRescuers", new
            {
                Count = rescuers.Count,
                RescuerIds = rescuers
            });
        }
    }
}