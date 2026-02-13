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
using SnakeAid.Repository.Interfaces;
using SnakeAid.Repository.Data;

namespace SnakeAid.Api.Hubs
{
    public class RescuerHub : Hub
    {
        private readonly IRescueRequestSessionService _sessionService;
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<RescuerHub> _logger;
        private readonly IRescuerLocationService _rescuerLocationService;

        // Static dictionary để track connected rescuers: userId -> connectionId
        public static ConcurrentDictionary<string, string> ConnectedRescuers => SignalRRescueNotificationService.ConnectedRescuers;

        public RescuerHub(
            IRescueRequestSessionService sessionService,
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<RescuerHub> logger,
            IRescuerLocationService rescuerLocationService)
        {
            _sessionService = sessionService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _rescuerLocationService = rescuerLocationService;
        }

        /// <summary>
        /// Khi rescuer connect và join để nhận requests
        /// </summary>
        public async Task JoinAsRescuer(string userId)
        {
            _logger.LogInformation("JoinAsRescuer called for userId: {UserId}, ConnectionId: {ConnectionId}, Current dictionary size: {DictSize}",
                userId, Context.ConnectionId, SignalRRescueNotificationService.ConnectedRescuers.Count);

            // Add connection to notification service
            SignalRRescueNotificationService.AddConnection(userId, Context.ConnectionId);

            _logger.LogInformation("After AddConnection: Dictionary size: {DictSize}, Contains {UserId}: {Contains}",
                SignalRRescueNotificationService.ConnectedRescuers.Count,
                userId,
                SignalRRescueNotificationService.ConnectedRescuers.ContainsKey(userId));

            // Update RescuerProfile IsOnline status in database
            if (Guid.TryParse(userId, out var rescuerGuid))
            {
                var rescuerProfile = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                    predicate: r => r.AccountId == rescuerGuid,
                    asNoTracking: false
                );

                if (rescuerProfile != null)
                {
                    rescuerProfile.IsOnline = true;
                    rescuerProfile.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<RescuerProfile>().Update(rescuerProfile);
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Rescuer {UserId} set to ONLINE in database", userId);
                }
                else
                {
                    _logger.LogWarning("RescuerProfile not found for userId: {UserId}", userId);
                }
            }
            else
            {
                _logger.LogWarning("Invalid GUID format for userId: {UserId}", userId);
            }

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


        public async Task UpdateLocation(string userId, double latitude, double longitude)
        {
            // Update location in DB via service (LT-1)
            if (Guid.TryParse(userId, out var rescuerGuid))
            {
                await _rescuerLocationService.UpdateLocationAsync(rescuerGuid, latitude, longitude, null, null, null);
            }
            else
            {
                _logger.LogWarning("Invalid GUID format for userId: {UserId}", userId);
            }

            _logger.LogInformation("Rescuer {UserId} updated location: {Lat}, {Lng}", userId, latitude, longitude);
            
            // Echo back to client (legacy behavior)
            // TODO: In LT-2, this might be replaced by session group broadcast
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
            var connectionId = Context.ConnectionId;

            _logger.LogWarning("⚠️ OnDisconnectedAsync START. ConnectionId: {ConnectionId}, Dictionary size: {DictSize}, Exception: {Exception}",
                connectionId, SignalRRescueNotificationService.ConnectedRescuers.Count, exception?.Message ?? "None");

            // Log all current connections
            _logger.LogWarning("Current connections: {Connections}",
                string.Join(", ", SignalRRescueNotificationService.ConnectedRescuers.Select(kvp => $"{kvp.Key}→{kvp.Value}")));

            var userId = ConnectedRescuers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (userId != null)
            {
                _logger.LogWarning("Found userId {UserId} for disconnected ConnectionId {ConnectionId}", userId, connectionId);

                SignalRRescueNotificationService.RemoveConnection(userId);

                _logger.LogWarning("After removal, Dictionary size: {DictSize}", SignalRRescueNotificationService.ConnectedRescuers.Count);

                // Update RescuerProfile IsOnline status in database
                if (Guid.TryParse(userId, out var rescuerGuid))
                {
                    var rescuerProfile = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                        predicate: r => r.AccountId == rescuerGuid,
                        asNoTracking: false
                    );

                    if (rescuerProfile != null)
                    {
                        rescuerProfile.IsOnline = false;
                        rescuerProfile.UpdatedAt = DateTime.UtcNow;
                        rescuerProfile.LastLocationUpdate = DateTime.UtcNow;
                        _unitOfWork.GetRepository<RescuerProfile>().Update(rescuerProfile);
                        await _unitOfWork.CommitAsync();
                        _logger.LogWarning("✅ Rescuer {UserId} set to OFFLINE in database due to disconnection", userId);
                    }
                }
            }
            else
            {
                _logger.LogWarning("⚠️ OnDisconnectedAsync called for UNKNOWN connection {ConnectionId} (never called JoinAsRescuer?)", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);

            _logger.LogWarning("⚠️ OnDisconnectedAsync END. Final dictionary size: {DictSize}", SignalRRescueNotificationService.ConnectedRescuers.Count);
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