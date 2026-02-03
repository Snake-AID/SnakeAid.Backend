using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.RescueRequestSession;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.RescueRequestSession;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

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

        public async Task<CreateIncidentResponse> CancelIncidentAsync(Guid incidentId)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var existingIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: s => s.Id == incidentId
                    );
                if (existingIncident == null)
                {
                    throw new NotFoundException("Snakebite incident not found.");
                }
                // Validate incident status - only allow cancelling for Pending incidents
                if (existingIncident.Status != SnakebiteIncidentStatus.Pending && existingIncident.Status != SnakebiteIncidentStatus.Assigned)
                {
                    throw new BadRequestException($"Cannot cancel incident with status: {existingIncident.Status}");
                }
                existingIncident.Status = SnakebiteIncidentStatus.Cancelled;
                _unitOfWork.GetRepository<SnakebiteIncident>().Update(existingIncident);
                await _unitOfWork.CommitAsync();
                return existingIncident.Adapt<CreateIncidentResponse>();
            });
        }

        public async Task<CreateIncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid userId)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
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

                return responseData;
            });
        }

        public async Task<ApiResponse<DetailSnakebiteIncidentReposne>> GetDetailIncidentAsync(Guid incidentId)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var existingIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                            predicate: s => s.Id == incidentId,
                            include: query => query
                                .Include(i => i.User)
                                .Include(i => i.AssignedRescuer)
                                .Include(i => i.Sessions)
                                .Include(i => i.AllRequests)
                                    .ThenInclude(r => r.Rescuer)
                                .Include(i => i.RescueMission)
                                .Include(i => i.Media)
                        );

                    if (existingIncident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    var responseData = existingIncident.Adapt<DetailSnakebiteIncidentReposne>();

                    return ApiResponseBuilder.BuildSuccessResponse(responseData, "Snakebite Incident details retrieved successfully!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving snakebite incident details: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<ApiResponse<CreateIncidentResponse>> RaiseSessionRangeAsync(RaiseSessionRangeRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Request data cannot be null.");
                }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Load incident with all required navigation properties
                var existingIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: s => s.Id == request.IncidentId,
                        include: query => query
                            .Include(i => i.Sessions)
                            .Include(i => i.User)
                    );

                if (existingIncident == null)
                {
                    throw new NotFoundException("Snakebite incident not found.");
                }

                // Validate incident status - only allow raising range for Pending incidents
                if (existingIncident.Status != SnakebiteIncidentStatus.Pending)
                {
                    throw new BadRequestException($"Cannot raise session range for incident with status: {existingIncident.Status}");
                }

                // Close current session as Failed
                var currentSession = existingIncident.Sessions
                    .FirstOrDefault(s => s.SessionNumber == existingIncident.CurrentSessionNumber);

                if (currentSession != null)
                {
                    currentSession.Status = SessionStatus.Failed;
                    currentSession.CompletedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<RescueRequestSession>().Update(currentSession);
                }

                // Check if maximum sessions reached (max 3 sessions)
                if (existingIncident.CurrentSessionNumber >= 3)
                {
                    existingIncident.Status = SnakebiteIncidentStatus.NoRescuerFound;
                    existingIncident.LastSessionAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(existingIncident);
                    await _unitOfWork.CommitAsync();

                    throw new BadRequestException("Maximum session range expansions reached (3 sessions). No rescuers found in area.");
                }

                // Calculate new radius with progressive increment (5km -> 7km -> 10km)
                int radiusIncrement = existingIncident.CurrentSessionNumber switch
                {
                    1 => 2,  // Session 1 (5km) -> Session 2 (7km)
                    2 => 3, // Session 2 (7km) -> Session 3 (10km)
                    _ => 5   // Fallback
                };

                int newRadius = existingIncident.CurrentRadiusKm + radiusIncrement;

                // Update incident for new session
                existingIncident.CurrentSessionNumber += 1;
                existingIncident.CurrentRadiusKm = newRadius;
                existingIncident.LastSessionAt = DateTime.UtcNow;

                // Create new session
                var newSession = new RescueRequestSession
                {
                    Id = Guid.NewGuid(),
                    IncidentId = existingIncident.Id,
                    SessionNumber = existingIncident.CurrentSessionNumber,
                    RadiusKm = existingIncident.CurrentRadiusKm,
                    Status = SessionStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    TriggerType = SessionTrigger.RadiusExpanded,
                    RescuersPinged = 0
                };

                await _unitOfWork.GetRepository<RescueRequestSession>().InsertAsync(newSession);
                await _unitOfWork.CommitAsync();

                // Reload sessions collection from DB to ensure consistency and proper order
                await _unitOfWork.Context.Entry(existingIncident)
                    .Collection(i => i.Sessions)
                    .LoadAsync();

                var responseData = existingIncident.Adapt<CreateIncidentResponse>();
                // Manually set LocationCoordinates since Mapster doesn't handle NetTopologySuite Point
                responseData.LocationCoordinates = existingIncident.LocationCoordinates;
                responseData.Sessions = existingIncident.Sessions
                    .OrderBy(s => s.SessionNumber)
                    .Select(s => s.Adapt<CreateRescueRequestSessionResponse>())
                    .ToList();

                return responseData;
            });
        }

        public async Task<UpdateSymptomReportResponse> UpdateSymptomReportAsync(Guid incidentId, UpdateSymptomReportRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var existingIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: s => s.Id == incidentId
                    );
                if (existingIncident == null)
                {
                    throw new NotFoundException("Snakebite incident not found.");
                }

                // Calculate elapsed time from incident occurrence
                var currentTime = DateTime.UtcNow;
                var elapsedMinutes = existingIncident.IncidentOccurredAt.HasValue
                    ? (int)(currentTime - existingIncident.IncidentOccurredAt.Value).TotalMinutes
                    : 0;

                // Collect symptom descriptions and calculate severity
                var symptomDescriptions = new List<string>();
                var coreSymptomScores = new List<int>();
                var modifierSymptomScores = new List<int>();

                foreach (var symptomId in request.SymptomIdList)
                {
                    var symptom = await _unitOfWork.GetRepository<SymptomConfig>().FirstOrDefaultAsync(
                        predicate: s => s.Id == symptomId
                    );

                    if (symptom != null)
                    {
                        // Add symptom description
                        if (!string.IsNullOrEmpty(symptom.Description))
                        {
                            symptomDescriptions.Add(symptom.Description);
                        }

                        // Calculate score based on TimeScoreList
                        var score = CalculateScoreByElapsedTime(symptom.TimeScoreList, elapsedMinutes);

                        // Categorize by symptom category
                        if (symptom.Category == SymptomCategory.Core)
                        {
                            coreSymptomScores.Add(score);
                        }
                        else if (symptom.Category == SymptomCategory.Modifier)
                        {
                            modifierSymptomScores.Add(score);
                        }
                    }
                }

                // Calculate severity level
                // Core: take maximum score
                var severityLevel = 0;
                if (coreSymptomScores.Any())
                {
                    severityLevel = coreSymptomScores.Max();
                }

                // Modifier: sum all scores
                if (modifierSymptomScores.Any())
                {
                    severityLevel += modifierSymptomScores.Sum();
                }

                if (severityLevel > 100)
                    severityLevel = 100;

                // Update symptom report and severity level
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = false
                };
                existingIncident.SymptomsReport = System.Text.Json.JsonSerializer.Serialize(symptomDescriptions, jsonOptions);
                existingIncident.SeverityLevel = severityLevel;
                _unitOfWork.GetRepository<SnakebiteIncident>().Update(existingIncident);
                await _unitOfWork.CommitAsync();

                return existingIncident.Adapt<UpdateSymptomReportResponse>();
            });
        }

        /// <summary>
        /// Calculate score based on elapsed time and TimeScoreList ranges
        /// </summary>
        /// <param name="timeScoreList">List of time score ranges</param>
        /// <param name="elapsedMinutes">Minutes elapsed since incident occurred</param>
        /// <returns>Score matching the elapsed time range, or 0 if no match found</returns>
        private int CalculateScoreByElapsedTime(List<TimeScorePoint> timeScoreList, int elapsedMinutes)
        {
            if (timeScoreList == null || !timeScoreList.Any())
            {
                return 0;
            }

            // Find the TimeScorePoint where elapsedMinutes falls within MinMinutes and MaxMinutes
            var matchingScore = timeScoreList.FirstOrDefault(ts =>
                elapsedMinutes >= ts.MinMinutes && elapsedMinutes <= ts.MaxMinutes
            );

            return matchingScore?.Score ?? 0;
        }
    }
}
