using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Repository.Data;
using SnakeAid.Core.Domains;
using Microsoft.EntityFrameworkCore;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Hubs
{
    public class MissionHub : Hub
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<MissionHub> _logger;
        private readonly IRescuerLocationService _rescuerLocationService;

        public MissionHub(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<MissionHub> logger,
            IRescuerLocationService rescuerLocationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _rescuerLocationService = rescuerLocationService;
        }

        /// <summary>
        /// Validates and authorizes an incoming connection, stores incident context, and adds the connection to the incident group.
        /// </summary>
        /// <remarks>
        /// The method retrieves the incident ID from the HTTP query and the user identifier from the connection context,
        /// verifies both values, ensures the user is either the incident owner or the assigned rescuer, stores the incident ID
        /// in Context.Items, and adds the connection to the SignalR group named for the incident. If validation or authorization
        /// fails, the connection is aborted.
        /// </remarks>
        /// <returns>A task that completes when the connection handling has finished.</returns>
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var incidentIdString = httpContext?.Request.Query["incidentId"];

            if (string.IsNullOrEmpty(incidentIdString) || !Guid.TryParse(incidentIdString, out var incidentId))
            {
                _logger.LogWarning("Connection rejected: Missing or invalid incidentId.");
                Context.Abort();
                return;
            }

            var userIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Connection rejected: Unauthenticated user or invalid UserIdentifier. Value: '{UserIdentifier}'", userIdString ?? "NULL");
                Context.Abort();
                return;
            }

            // Verify authorization: User must be member or assigned rescuer
            var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                predicate: i => i.Id == incidentId
            );

            if (incident == null)
            {
                _logger.LogWarning("Connection rejected: Incident {IncidentId} not found.", incidentId);
                Context.Abort();
                return;
            }

            if (incident.UserId != userId && incident.AssignedRescuerId != userId)
            {
                _logger.LogWarning("Connection rejected: User {UserId} not authorized for incident {IncidentId}.", userId, incidentId);
                Context.Abort();
                return;
            }

            Context.Items["IncidentId"] = incidentId;
            await Groups.AddToGroupAsync(Context.ConnectionId, incidentId.ToString());
            _logger.LogInformation("User {UserId} joined MissionHub for Incident {IncidentId}", userId, incidentId);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Updates the caller's rescuer location for the specified incident and broadcasts a LocationUpdated message to the incident group.
        /// </summary>
        /// <param name="incidentId">Identifier of the incident (group) the update applies to.</param>
        /// <param name="latitude">Latitude in decimal degrees of the rescuer's location.</param>
        /// <param name="longitude">Longitude in decimal degrees of the rescuer's location.</param>
        public async Task UpdateLocation(Guid incidentId, double latitude, double longitude)
        {
            var userIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Operation UpdateLocation rejected: Invalid or missing UserIdentifier. Value: '{UserIdentifier}'", userIdString ?? "NULL");
                Context.Abort();
                return;
            }

            // Verify authorization: Caller must be authorized for this specific incidentId
            if (!Context.Items.TryGetValue("IncidentId", out var authorizedIdObj) ||
                authorizedIdObj is not Guid authorizedId ||
                authorizedId != incidentId)
            {
                _logger.LogWarning("Operation UpdateLocation rejected: User {UserId} is not authorized for Incident {IncidentId}. Authorized Incident: {AuthorizedId}",
                    userId, incidentId, authorizedIdObj ?? "NONE");
                Context.Abort();
                return;
            }

            // Update in DB
            await _rescuerLocationService.UpdateLocationAsync(userId, latitude, longitude, null, null, null);

            // Broadcast to Group (Member and Rescuer)
            await Clients.Group(incidentId.ToString()).SendAsync("LocationUpdated", new
            {
                UserId = userIdString,
                Latitude = latitude,
                Longitude = longitude,
                UpdatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Mission location update: Rescuer {UserId} in Incident {IncidentId}", userId, incidentId);
        }
    }
}
