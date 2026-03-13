using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Extensions;
using SnakeAid.Core.Responses.SnakeSpecies;
using SnakeAid.Core.Meta;

namespace SnakeAid.Service.Implements
{
    public class SnakebiteIncidentService : ISnakebiteIncidentService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakebiteIncidentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IOperatorRealtimeNotificationService _operatorRealtimeNotificationService;
        private readonly IRescueNotificationService _rescueNotificationService;

        public SnakebiteIncidentService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakebiteIncidentService> logger,
            IConfiguration configuration,
            IOperatorRealtimeNotificationService operatorRealtimeNotificationService,
            IRescueNotificationService rescueNotificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _operatorRealtimeNotificationService = operatorRealtimeNotificationService;
            _rescueNotificationService = rescueNotificationService;
        }

        public async Task<CreateIncidentResponse> ClaimIncidentAsync(Guid incidentId, Guid operatorId)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var operatorAccount = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                        predicate: a => a.Id == operatorId
                    );

                    if (operatorAccount == null)
                    {
                        throw new NotFoundException("Operator account not found.");
                    }

                    if (operatorAccount.Role != AccountRole.Operator && operatorAccount.Role != AccountRole.Admin)
                    {
                        throw new ForbiddenException("Only operators can claim incidents.");
                    }

                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId,
                        asNoTracking: false
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    if (incident.HandlingOperatorId.HasValue)
                    {
                        throw new ConflictException("Incident is already claimed by another operator.");
                    }

                    if (incident.Status != SnakebiteIncidentStatus.Pending)
                    {
                        throw new ConflictException($"Incident is no longer claimable with status: {incident.Status}");
                    }

                    incident.HandlingOperatorId = operatorId;
                    incident.Status = SnakebiteIncidentStatus.OperatorContacting;
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    return incident.Adapt<CreateIncidentResponse>();
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while claiming incident {IncidentId}", incidentId);
                throw new ConflictException("Incident was claimed or updated by another operator. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<CreateIncidentResponse> ConfirmIncidentAsync(Guid incidentId, Guid operatorId)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    if (incident.HandlingOperatorId != operatorId)
                    {
                        throw new ConflictException("Incident is being handled by another operator.");
                    }

                    if (incident.Status != SnakebiteIncidentStatus.OperatorContacting && incident.Status != SnakebiteIncidentStatus.Verified)
                    {
                        throw new BadRequestException($"Cannot confirm incident with status: {incident.Status}");
                    }

                    if (incident.Status == SnakebiteIncidentStatus.Verified)
                    {
                        return incident.Adapt<CreateIncidentResponse>();
                    }

                    incident.Status = SnakebiteIncidentStatus.Verified;
                    incident.ConfirmedAt = DateTime.UtcNow;
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    return incident.Adapt<CreateIncidentResponse>();
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while confirming incident {IncidentId}", incidentId);
                throw new ConflictException("Incident was updated by another operator. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
        }

        public async Task<CreateIncidentResponse> DispatchIncidentAsync(Guid incidentId, Guid rescuerId, Guid operatorId)
        {
            try
            {
                Guid dispatchRequestId = Guid.Empty;
                DateTime dispatchedAt = DateTime.UtcNow;
                double incidentLatitude = 0;
                double incidentLongitude = 0;

                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var incident = await _unitOfWork.GetRepository<SnakebiteIncident>().FirstOrDefaultAsync(
                        predicate: i => i.Id == incidentId
                    );

                    if (incident == null)
                    {
                        throw new NotFoundException("Snakebite incident not found.");
                    }

                    if (incident.HandlingOperatorId != operatorId)
                    {
                        throw new ConflictException("Incident is being handled by another operator.");
                    }

                    if (incident.Status != SnakebiteIncidentStatus.Verified)
                    {
                        throw new BadRequestException($"Cannot dispatch incident with status: {incident.Status}");
                    }

                    var rescuer = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                        predicate: r => r.AccountId == rescuerId
                    );

                    if (rescuer == null)
                    {
                        throw new NotFoundException("Rescuer not found.");
                    }

                    var dispatchRequest = new RescuerRequest
                    {
                        Id = Guid.NewGuid(),
                        IncidentId = incidentId,
                        RescuerId = rescuer.AccountId,
                        OperatorId = operatorId,
                        Status = RescueRequestStatus.Pending,
                        DispatchedAt = dispatchedAt,
                        ResponseAt = null,
                        DeclineReason = null
                    };

                    await _unitOfWork.GetRepository<RescuerRequest>().InsertAsync(dispatchRequest);

                    // Keep incident in Verified until rescuer acknowledges the dispatch.
                    incident.Status = SnakebiteIncidentStatus.Verified;
                    incident.DispatchedAt = dispatchedAt;
                    _unitOfWork.GetRepository<SnakebiteIncident>().Update(incident);

                    dispatchRequestId = dispatchRequest.Id;
                    incidentLatitude = incident.LocationCoordinates.Y;
                    incidentLongitude = incident.LocationCoordinates.X;

                    return incident.Adapt<CreateIncidentResponse>();
                });

                await _rescueNotificationService.NotifyDispatchRequestedAsync(rescuerId.ToString(), new
                {
                    RequestId = dispatchRequestId,
                    IncidentId = incidentId,
                    OperatorId = operatorId,
                    RescuerId = rescuerId,
                    DispatchedAt = dispatchedAt,
                    Latitude = incidentLatitude,
                    Longitude = incidentLongitude,
                    Message = "Operator assigned a dispatch request. Please acknowledge if you can take this case."
                });

                return response;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while dispatching incident {IncidentId}", incidentId);
                throw new ConflictException("Incident was updated by another operator. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching incident {IncidentId}: {Message}", incidentId, ex.Message);
                throw;
            }
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

                var responseData = await _unitOfWork.ExecuteInTransactionAsync(async () =>
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
                    IncidentOccurredAt = DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<SnakebiteIncident>().InsertAsync(newIncident);
                await _unitOfWork.CommitAsync();

                return newIncident.Adapt<CreateIncidentResponse>();
            });

                // Best-effort realtime notify for operator dashboard map.
                await _operatorRealtimeNotificationService.NotifyNewIncidentCreatedAsync(
                    responseData.Id,
                    userId,
                    request.Lat,
                    request.Lng);

                return responseData;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating snakebite incident: {Message}", ex.Message);
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

                    // using the time by miniute in request if provided, otherwise caculate from time provided in Incident
                    var elapsedMinutes = request.TimeSinceBiteMinutes ?? (existingIncident.IncidentOccurredAt.HasValue
                        ? (int)(DateTime.UtcNow - existingIncident.IncidentOccurredAt.Value).TotalMinutes
                        : 0);

                    // Collect symptom descriptions and calculate severity
                    var reportedSymptoms = new List<ReportSymptom>();
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
                                reportedSymptoms.Add(new ReportSymptom
                                {
                                    SymptomId = symptom.Id,
                                    SymptomName = symptom.Name,
                                    SymptomDescription = symptom.Description
                                });
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
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions()
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = false
                    };
                    existingIncident.SymptomsReport = reportedSymptoms;
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
                            asNoTracking: false,
                            cancellationToken: default
                        );

                    if (recognitionResult == null)
                    {
                        _logger.LogWarning("Recognition result not found: {ResultId}", recognitionResultId);
                        throw new NotFoundException("Recognition result not found.");
                    }

                    // Explicitly load related entities
                    await _unitOfWork.Context.Entry(recognitionResult)
                        .Reference(r => r.ReportMedia)
                        .LoadAsync();

                    if (recognitionResult.DetectedSpeciesId.HasValue)
                    {
                        await _unitOfWork.Context.Entry(recognitionResult)
                            .Reference(r => r.DetectedSpecies)
                            .LoadAsync();
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

        public Task<PagedData<DetailSnakebiteIncidentResponse>> GetUserIncidentsAsync(Guid userId, SnakebiteIncidentStatus? status, int page, int pageSize)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<SnakebiteIncident>();
                var userIncidents = repo.GetPagingListAsync<DetailSnakebiteIncidentResponse>(
                    predicate: i => i.UserId == userId &&
                                    (!status.HasValue || i.Status == status.Value),
                    page: page,
                    size: pageSize,
                    orderBy: q => q.OrderByDescending(i => i.CreatedAt),
                    selector: i => i.Adapt<DetailSnakebiteIncidentResponse>()
                );
                return userIncidents;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user incidents for user {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }
    }
}
