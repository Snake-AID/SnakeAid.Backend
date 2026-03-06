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
using SnakeAid.Service.Extensions;
using SnakeAid.Core.Responses.SymptomConfig;

namespace SnakeAid.Service.Implements
{
    public class SnakebiteIncidentService : ISnakebiteIncidentService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakebiteIncidentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IRescueRequestSessionService _sessionService;
        private const int MAX_SESSIONS = 3;
        private const int REQUEST_TIMEOUT_SECONDS = 60;
        private static readonly int[] RADIUS_PROGRESSION = { 10, 20, 30 }; // km

        public SnakebiteIncidentService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakebiteIncidentService> logger,
            IConfiguration configuration,
            IRescueRequestSessionService sessionService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _sessionService = sessionService;
        }

        public async Task<CreateIncidentResponse> CancelIncidentAsync(Guid incidentId)
        {
            try
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
                    // only in Pending or Assigned status can be cancelled
                    if (existingIncident.Status != SnakebiteIncidentStatus.Pending && existingIncident.Status != SnakebiteIncidentStatus.Assigned)
                    {
                        throw new BadRequestException($"Cannot cancel incident with status: {existingIncident.Status}");
                    }
                    existingIncident.Status = SnakebiteIncidentStatus.Cancelled;
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(existingIncident);

                    var responseData = existingIncident.Adapt<CreateIncidentResponse>();
                    return responseData;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling snakebite incident: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<CreateIncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid userId)
        {
            try
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
                    CurrentSessionNumber = 0, // Will be set when first session is created
                    CurrentRadiusKm = 0,     // Will be set when first session is created  
                    LastSessionAt = null,    // Will be set when first session is created
                    IncidentOccurredAt = DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<SnakebiteIncident>().InsertAsync(newIncident);
                await _unitOfWork.CommitAsync();

                var responseData = newIncident.Adapt<CreateIncidentResponse>();
                responseData.Sessions = new List<CreateRescueRequestSessionResponse>();

                return responseData;
            });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating snakebite incident: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<CreateIncidentResponse> RaiseSessionRangeAsync(RaiseSessionRangeRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new BadRequestException("Request data cannot be null.");
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Load incident with all required navigation properties
                    var existingIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                            predicate: s => s.Id == request.IncidentId,
                            include: query => query
                                .Include(i => i.Sessions)
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
                    // Check max session (trước khi tăng)
                    if (existingIncident.CurrentSessionNumber >= RADIUS_PROGRESSION.Length)
                    {
                        existingIncident.Status = SnakebiteIncidentStatus.NoRescuerFound;
                        existingIncident.LastSessionAt = DateTime.UtcNow;
                        _unitOfWork.GetRepository<SnakebiteIncident>().Update(existingIncident);
                        await _unitOfWork.CommitAsync();

                        throw new BadRequestException(
                            $"Maximum session range expansions reached ({RADIUS_PROGRESSION.Length} sessions). No rescuers found."
                        );
                    }

                    existingIncident.CurrentSessionNumber += 1;

                    int radiusIndex = existingIncident.CurrentSessionNumber - 1;
                    int newRadius = RADIUS_PROGRESSION[radiusIndex];

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
                    responseData.Sessions = existingIncident.Sessions
                        .OrderBy(s => s.SessionNumber)
                        .Select(s => s.Adapt<CreateRescueRequestSessionResponse>())
                        .ToList();

                    return responseData;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving snakebite incident details: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<DetailSnakebiteIncidentResponse> GetDetailIncidentAsync(Guid incidentId)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var existingIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                            predicate: s => s.Id == incidentId,
                            include: query => query
                                .Include(i => i.User)
                                    .ThenInclude(u => u.Account)
                                .Include(i => i.AssignedRescuer)
                                    .ThenInclude(r => r.Account)
                                .Include(i => i.Missions)
                                .Include(i => i.IdentifiedSnakeSpecies)
                                .Include(i => i.AIRecognitionResult)
                        );

                    if (existingIncident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    await existingIncident.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakebiteIncident);

                    _logger.LogInformation("Loaded {MediaCount} media items for incident {IncidentId}",
                        existingIncident.Media?.Count ?? 0, incidentId);

                    var responseData = existingIncident.Adapt<DetailSnakebiteIncidentResponse>();

                    // Map identified snake manually if available
                    if (existingIncident.IdentifiedSnakeSpecies != null)
                    {
                        responseData.IdentifiedSnake = existingIncident.IdentifiedSnakeSpecies.Adapt<SnakeSpeciesResponse>();

                        responseData.IdentificationContext = new Core.Responses.FirstAid.SnakeIdentificationContext
                        {
                            Method = existingIncident.IdentificationMethod,
                            IdentifiedAt = existingIncident.IdentifiedAt ?? DateTime.UtcNow
                        };

                        // Add AI confidence if applicable
                        if (existingIncident.IdentificationMethod == SnakeIdentificationMethod.AIDetection
                            && existingIncident.AIRecognitionResult != null)
                        {
                            responseData.IdentificationContext.AIConfidence = (float)existingIncident.AIRecognitionResult.Confidence;
                        }
                    }

                    _logger.LogInformation("Mapped {MediaCount} media items in response for incident {IncidentId}",
                        responseData.Media?.Count ?? 0, incidentId);

                    return responseData;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving snakebite incident details: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<UpdateSymptomReportResponse> UpdateSymptomReportAsync(Guid incidentId, UpdateSymptomReportRequest request)
        {
            try
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

                    // Attach media before mapping to response
                    await existingIncident.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakebiteIncident);

                    var responseData = existingIncident.Adapt<UpdateSymptomReportResponse>();
                    return responseData;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating symptom report: {Message}", ex.Message);
                throw;
            }
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


        public async Task<TriggerRescueResponse> TriggerRescueAsync(Guid incidentId)
        {
            try
            {
                // Validate incident exists and is pending
                var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                    predicate: i => i.Id == incidentId
                );

                if (incident == null)
                {
                    throw new NotFoundException("Incident not found.");
                }

                if (incident.Status != SnakebiteIncidentStatus.Pending)
                {
                    throw new BadRequestException($"Cannot trigger rescue for incident with status: {incident.Status}");
                }

                // Note: Actual session creation and broadcast will be handled by RescueRequestSessionService
                // This method is called from Controller, which should also call RescueRequestSessionService.StartRescueSessionAsync

                return new TriggerRescueResponse
                {
                    IncidentId = incidentId,
                    SessionId = Guid.Empty, // Will be set by session service
                    SessionNumber = 1,
                    RadiusKm = 10,
                    RescuersPinged = 0,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Rescue session triggered, broadcasting to nearby rescuers."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering rescue for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        /// Handle rescuer accept - delegate to RescueRequestSessionService
        public async Task<AcceptRescueResponse> AcceptRescueAsync(Guid requestId, Guid rescuerId)
        {
            try
            {
                // Get request to return info
                var request = await _unitOfWork.GetRepository<RescuerRequest>().FirstOrDefaultAsync(
                    predicate: r => r.Id == requestId && r.RescuerId == rescuerId
                );

                if (request == null)
                {
                    throw new NotFoundException("Request not found or not assigned to this rescuer.");
                }

                // Note: Actual accept logic will be handled by RescueRequestSessionService.AcceptRequestAsync
                // This is just validation and response building

                return new AcceptRescueResponse
                {
                    RequestId = requestId,
                    IncidentId = request.IncidentId,
                    RescuerId = rescuerId,
                    MissionId = Guid.Empty, // Will be set by session service
                    AcceptedAt = DateTime.UtcNow,
                    Message = "Request accepted successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting rescue request {RequestId}: {Message}", requestId, ex.Message);
                throw;
            }
        }


        /// Start rescue session for existing incident (tạo session và broadcast qua SignalR)
        public async Task<TriggerRescueResponse> StartRescueAsync(Guid incidentId)
        {
            try
            {
                // Validate incident exists and is pending
                var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                    predicate: i => i.Id == incidentId
                );

                if (incident == null)
                {
                    throw new NotFoundException("Incident not found.");
                }

                if (incident.Status != SnakebiteIncidentStatus.Pending)
                {
                    throw new BadRequestException($"Cannot start rescue for incident with status: {incident.Status}");
                }

                // Delegate to session service to create session and broadcast
                await _sessionService.StartRescueSessionAsync(incidentId);

                // Get updated incident info
                var updatedIncident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                    predicate: i => i.Id == incidentId,
                    include: q => q.Include(i => i.Sessions.OrderByDescending(s => s.SessionNumber).Take(1))
                );

                var latestSession = updatedIncident?.Sessions?.FirstOrDefault();

                return new TriggerRescueResponse
                {
                    IncidentId = incidentId,
                    SessionId = latestSession?.Id ?? Guid.Empty,
                    SessionNumber = latestSession?.SessionNumber ?? 1,
                    RadiusKm = latestSession?.RadiusKm ?? 10,
                    RescuersPinged = latestSession?.RescuersPinged ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Rescue session started, broadcasting to nearby rescuers."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting rescue for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<object> GetMediaDebugInfoAsync(Guid incidentId)
        {
            try
            {
                // Get all media for this incident using raw query
                var allMediaForIncident = await _unitOfWork.GetRepository<ReportMedia>()
                    .GetListAsync(
                        predicate: m => m.ReferenceId == incidentId && m.ReferenceType == MediaReferenceType.SnakebiteIncident
                    );

                // Get all media with any reference to this ID (regardless of type)
                var allMediaWithThisId = await _unitOfWork.GetRepository<ReportMedia>()
                    .GetListAsync(
                        predicate: m => m.ReferenceId == incidentId
                    );

                // Get all media for SnakebiteIncident type
                var allSnakebiteMedia = await _unitOfWork.GetRepository<ReportMedia>()
                    .GetListAsync(
                        predicate: m => m.ReferenceType == MediaReferenceType.SnakebiteIncident
                    );

                return new
                {
                    IncidentId = incidentId,
                    MediaForThisIncident = allMediaForIncident.Select(m => new
                    {
                        m.Id,
                        m.ReferenceId,
                        m.ReferenceType,
                        m.Purpose,
                        m.MediaUrl,
                        m.RequiresAIProcessing,
                        m.IsProcessed
                    }).ToList(),
                    CountForThisIncident = allMediaForIncident.Count,
                    MediaWithThisIdAnyType = allMediaWithThisId.Select(m => new
                    {
                        m.Id,
                        m.ReferenceId,
                        m.ReferenceType
                    }).ToList(),
                    AllSnakebiteIncidentMedia = allSnakebiteMedia.Select(m => new
                    {
                        m.Id,
                        m.ReferenceId,
                        m.ReferenceType
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media debug info for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<IdentifySnakeResponse> IdentifySnakeByAIAsync(Guid incidentId, Guid recognitionResultId)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // 1. Validate incident exists
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>()
                        .FirstOrDefaultAsync(predicate: i => i.Id == incidentId);

                    if (incident == null)
                    {
                        _logger.LogWarning("Snakebite incident not found: {IncidentId}", incidentId);
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    // 2. Validate recognition result exists and belongs to this incident
                    var recognitionResult = await _unitOfWork.GetRepository<SnakeAIRecognitionResult>()
                        .FirstOrDefaultAsync(
                            predicate: r => r.Id == recognitionResultId,
                            include: query => query
                                .Include(r => r.ReportMedia)
                                .Include(r => r.DetectedSpecies)
                        );

                    if (recognitionResult == null)
                    {
                        _logger.LogWarning("Recognition result not found: {ResultId}", recognitionResultId);
                        throw new NotFoundException("Recognition result not found.");
                    }

                    // Verify the recognition result's media belongs to this incident
                    if (recognitionResult.ReportMedia == null ||
                        recognitionResult.ReportMedia.ReferenceId != incidentId ||
                        recognitionResult.ReportMedia.ReferenceType != MediaReferenceType.SnakebiteIncident)
                    {
                        _logger.LogWarning("Recognition result {ResultId} does not belong to incident {IncidentId}",
                            recognitionResultId, incidentId);
                        throw new BadRequestException("Recognition result does not belong to this incident.");
                    }

                    if (recognitionResult.DetectedSpeciesId == null || recognitionResult.DetectedSpecies == null)
                    {
                        _logger.LogWarning("Recognition result {ResultId} has no detected species", recognitionResultId);
                        throw new BadRequestException("No species detected in this recognition result.");
                    }

                    // 3. Update incident with identification
                    incident.IdentifiedSnakeSpeciesId = recognitionResult.DetectedSpeciesId.Value;
                    incident.IdentificationMethod = SnakeIdentificationMethod.AIDetection;
                    incident.AIRecognitionResultId = recognitionResultId;
                    incident.IdentifiedAt = DateTime.UtcNow;

                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    _logger.LogInformation("Snake identified for incident {IncidentId}: Species {SpeciesId} via AI with confidence {Confidence}",
                        incidentId, recognitionResult.DetectedSpeciesId, recognitionResult.Confidence);

                    return new IdentifySnakeResponse
                    {
                        IncidentId = incidentId,
                        IdentifiedSnakeSpeciesId = recognitionResult.DetectedSpeciesId.Value,
                        IdentificationMethod = SnakeIdentificationMethod.AIDetection,
                        IdentifiedAt = incident.IdentifiedAt.Value,
                        Snake = new SnakeSpeciesResponse
                        {
                            Id = recognitionResult.DetectedSpecies.Id,
                            ScientificName = recognitionResult.DetectedSpecies.ScientificName,
                            CommonName = recognitionResult.DetectedSpecies.CommonName ?? string.Empty,
                            Slug = recognitionResult.DetectedSpecies.Slug,
                            ImageUrl = recognitionResult.DetectedSpecies.ImageUrl,
                            Description = recognitionResult.DetectedSpecies.Description ?? string.Empty,
                            IdentificationSummary = recognitionResult.DetectedSpecies.IdentificationSummary ?? string.Empty,
                            PrimaryVenomType = recognitionResult.DetectedSpecies.PrimaryVenomType,
                            RiskLevel = recognitionResult.DetectedSpecies.RiskLevel,
                            IsVenomous = recognitionResult.DetectedSpecies.IsVenomous,
                            IsActive = recognitionResult.DetectedSpecies.IsActive
                        },
                        AIRecognitionResultId = recognitionResultId,
                        AIConfidence = (float)recognitionResult.Confidence
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error identifying snake by AI for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<IdentifySnakeResponse> IdentifySnakeByFilterAsync(Guid incidentId, IdentifyByFilterRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // 1. Validate incident exists
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>()
                        .FirstOrDefaultAsync(predicate: i => i.Id == incidentId);

                    if (incident == null)
                    {
                        _logger.LogWarning("Snakebite incident not found: {IncidentId}", incidentId);
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    // 2. Validate answers and get questions/options
                    var questionIds = request.Answers.Select(a => a.QuestionId).Distinct().ToList();
                    var questions = await _unitOfWork.GetRepository<FilterQuestion>()
                        .GetListAsync(
                            predicate: q => questionIds.Contains(q.Id) && q.IsActive,
                            include: query => query.Include(q => q.FilterOptions)
                        );

                    if (questions.Count != questionIds.Count)
                    {
                        throw new BadRequestException("Some questions are invalid or inactive.");
                    }

                    // 3. Find all snake species that match the selected options
                    var selectedOptionIds = request.Answers.Select(a => a.SelectedOptionId).ToList();

                    var mappings = await _unitOfWork.GetRepository<FilterSnakeMapping>()
                        .GetListAsync(
                            predicate: m => selectedOptionIds.Contains(m.FilterOptionId) && m.IsActive,
                            include: query => query
                                .Include(m => m.FilterOption)
                                .Include(m => m.SnakeSpecies)
                        );

                    // Group by snake species and count how many filter criteria match
                    var snakeMatches = mappings
                        .GroupBy(m => m.SnakeSpeciesId)
                        .Select(g => new
                        {
                            SnakeSpeciesId = g.Key,
                            MatchCount = g.Count(),
                            Species = g.First().SnakeSpecies
                        })
                        .OrderByDescending(s => s.MatchCount)
                        .ToList();

                    if (!snakeMatches.Any())
                    {
                        _logger.LogWarning("No snake species matched the filter answers for incident {IncidentId}", incidentId);
                        throw new BadRequestException("No snake species found matching the provided answers.");
                    }

                    // 4. Determine which snake to identify
                    int identifiedSpeciesId;
                    SnakeSpecies identifiedSpecies;

                    if (request.SelectedSnakeSpeciesId.HasValue)
                    {
                        // User explicitly selected from multiple matches
                        var selected = snakeMatches.FirstOrDefault(m => m.SnakeSpeciesId == request.SelectedSnakeSpeciesId.Value);
                        if (selected == null)
                        {
                            throw new BadRequestException("Selected snake species is not in the matched results.");
                        }
                        identifiedSpeciesId = selected.SnakeSpeciesId;
                        identifiedSpecies = selected.Species;
                    }
                    else
                    {
                        // Use the best match (highest match count)
                        var bestMatch = snakeMatches.First();
                        identifiedSpeciesId = bestMatch.SnakeSpeciesId;
                        identifiedSpecies = bestMatch.Species;
                    }

                    // 5. Build FilterAnswerData
                    var filterAnswerData = new FilterAnswerData
                    {
                        Answers = request.Answers.Select(a =>
                        {
                            var question = questions.First(q => q.Id == a.QuestionId);
                            var option = question.FilterOptions.First(o => o.Id == a.SelectedOptionId);
                            return new FilterAnswer
                            {
                                QuestionId = a.QuestionId,
                                QuestionText = question.Question,
                                SelectedOptionId = a.SelectedOptionId,
                                SelectedOptionText = option.OptionText
                            };
                        }).ToList(),
                        MatchedSnakeSpeciesIds = snakeMatches.Select(m => m.SnakeSpeciesId).ToList(),
                        SelectedSnakeSpeciesId = identifiedSpeciesId
                    };

                    // 6. Update incident with identification
                    incident.IdentifiedSnakeSpeciesId = identifiedSpeciesId;
                    incident.IdentificationMethod = SnakeIdentificationMethod.FilterQuestions;
                    incident.FilterAnswers = filterAnswerData;
                    incident.IdentifiedAt = DateTime.UtcNow;

                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    _logger.LogInformation("Snake identified for incident {IncidentId}: Species {SpeciesId} via filter questions with {MatchCount} matches",
                        incidentId, identifiedSpeciesId, snakeMatches.Count);

                    return new IdentifySnakeResponse
                    {
                        IncidentId = incidentId,
                        IdentifiedSnakeSpeciesId = identifiedSpeciesId,
                        IdentificationMethod = SnakeIdentificationMethod.FilterQuestions,
                        IdentifiedAt = incident.IdentifiedAt.Value,
                        Snake = new SnakeSpeciesResponse
                        {
                            Id = identifiedSpecies.Id,
                            ScientificName = identifiedSpecies.ScientificName,
                            CommonName = identifiedSpecies.CommonName ?? string.Empty,
                            Slug = identifiedSpecies.Slug,
                            ImageUrl = identifiedSpecies.ImageUrl,
                            Description = identifiedSpecies.Description ?? string.Empty,
                            IdentificationSummary = identifiedSpecies.IdentificationSummary ?? string.Empty,
                            PrimaryVenomType = identifiedSpecies.PrimaryVenomType,
                            RiskLevel = identifiedSpecies.RiskLevel,
                            IsVenomous = identifiedSpecies.IsVenomous,
                            IsActive = identifiedSpecies.IsActive
                        },
                        MatchedSnakes = snakeMatches.Select(m =>
                            $"{m.Species.CommonName ?? m.Species.ScientificName} ({m.MatchCount} matches)")
                            .ToList()
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error identifying snake by filter for incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }
    }
}
