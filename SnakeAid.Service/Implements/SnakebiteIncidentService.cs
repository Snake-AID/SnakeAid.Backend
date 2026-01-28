using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapster;
using SnakeAid.Core.Responses.RescueRequestSession;

namespace SnakeAid.Service.Implements
{
    public class SnakebiteIncidentService : ISnakebiteIncidentService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakebiteIncidentService> _logger;
        private readonly IConfiguration _configuration;

        public SnakebiteIncidentService(IUnitOfWork<SnakeAidDbContext> unitOfWork, ILogger<SnakebiteIncidentService> logger, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ApiResponse<CreateIncidentResponse>> CreateIncidentAsync(CreateIncidentRequest request, Guid userId)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Request data cannot be null.");
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var existingAccount = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                            predicate: a => a.Id == userId,
                            include: m => m.Include(i => i.MemberProfile)
                        );

                    if (existingAccount.MemberProfile == null)
                    {
                        throw new BadRequestException("Member information could not be found for the current account.");
                    }

                    // Create Point from lng/lat (PostGIS uses SRID 4326 - WGS84)
                    var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                    var locationPoint = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(request.Lng, request.Lat));

                    var newIncident = new SnakebiteIncident
                    {
                        Id = Guid.NewGuid(),
                        UserId = existingAccount.Id,
                        LocationCoordinates = locationPoint,
                        Status = SnakebiteIncidentStatus.Pending,
                        CurrentSessionNumber = 1,
                        CurrentRadiusKm = 5,
                        LastSessionAt = DateTime.UtcNow,
                        IncidentOccurredAt = DateTime.UtcNow
                    };

                    var firstRescueSession = new RescueRequestSession
                    {
                        Id = Guid.NewGuid(),
                        IncidentId = newIncident.Id,
                        SessionNumber = newIncident.CurrentSessionNumber,
                        RadiusKm = newIncident.CurrentRadiusKm,
                        Status = SessionStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        TriggerType = SessionTrigger.Initial,
                        RescuersPinged = 0
                    };

                    await _unitOfWork.GetRepository<SnakebiteIncident>().InsertAsync(newIncident);
                    await _unitOfWork.GetRepository<RescueRequestSession>().InsertAsync(firstRescueSession);
                    await _unitOfWork.CommitAsync();

                    var responseData = newIncident.Adapt<CreateIncidentResponse>();
                    responseData.Sessions = new List<CreateRescueRequestSessionResponse>
                    {
                        firstRescueSession.Adapt<CreateRescueRequestSessionResponse>()
                    };

                    return ApiResponseBuilder.BuildSuccessResponse(responseData, "Snakebite Incident created successfully!");
                });
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating snakebit incident: {Message}", ex.Message);
                throw;
            }
        }
    }
}
