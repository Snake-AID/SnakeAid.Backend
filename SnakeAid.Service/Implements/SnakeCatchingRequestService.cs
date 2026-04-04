using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Constants;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Requests.SnakeCatchingRequest;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.SnakeCatchingRequest;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.SnakeDetection;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Core.Services;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Extensions;
using SnakeAid.Service.Interfaces;
using SnakeAid.Core.Responses.SnakebiteIncident;

namespace SnakeAid.Service.Implements
{
    public class SnakeCatchingRequestService : ISnakeCatchingRequestService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakeCatchingRequestService> _logger;
        private readonly ISystemSettingService _systemSettingService;
        private readonly ILocationIqService _locationIqService;
        private readonly ISnakeAIService _snakeAIService;
        private readonly ISnakeCatchingRequestNotificationService _snakeCatchingRequestNotificationService;
        private const decimal ADDITIONAL_SNAKE_PRICE = 100000m;
        private const decimal TRANSFER_PRICE = 150000m;
        private const double DEFAULT_CENTER_LATITUDE = 10.8391267;
        private const double DEFAULT_CENTER_LONGITUDE = 106.8413534;

        public SnakeCatchingRequestService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakeCatchingRequestService> logger,
            ISystemSettingService systemSettingService,
            ILocationIqService locationIqService,
            ISnakeAIService snakeAIService,
            ISnakeCatchingRequestNotificationService snakeCatchingRequestNotificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _systemSettingService = systemSettingService;
            _locationIqService = locationIqService;
            _snakeAIService = snakeAIService;
            _snakeCatchingRequestNotificationService = snakeCatchingRequestNotificationService;
        }

        public async Task<CreateSnakeCatchingRequestResponse> CreateSnakeCatchingRequestAsync(
            Guid userId,
            CreateSnakeCatchingRequestRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new BadRequestException("Request data cannot be null.");
                }

                // Calculate estimated price based on distance from center (before creating request to avoid unnecessary calculations if validation fails)
                var estimatedPrice = await CalculateEstimatedPriceFromCenterAsync(
                    request.Lng,
                    request.Lat,
                    $"create request for user {userId}");


                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Validate user exists and has member profile
                    var existingAccount = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                        predicate: a => a.Id == userId,
                        include: m => m.Include(i => i.MemberProfile)
                    );

                    if (existingAccount == null)
                    {
                        throw new NotFoundException("Account not found.");
                    }

                    if (existingAccount.MemberProfile == null)
                    {
                        throw new BadRequestException("Member information could not be found for the current account.");
                    }

                    // Create Point from lng/lat (PostGIS uses SRID 4326 - WGS84)
                    // Coordinate(x, y) where x=longitude, y=latitude
                    var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                    var locationPoint = geometryFactory.CreatePoint(
                        new NetTopologySuite.Geometries.Coordinate(request.Lng, request.Lat));

                    _logger.LogInformation(
                        "Creating snake catching request at Lng={Lng}, Lat={Lat} (Point.X={X}, Point.Y={Y})",
                        request.Lng, request.Lat, locationPoint.X, locationPoint.Y);


                    // Create new SnakeCatchingRequest
                    var newRequest = new SnakeCatchingRequest
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Address = request.Address,
                        LocationCoordinates = locationPoint,
                        AdditionalDetails = request.AdditionalDetails,
                        Status = RequestStatus.Pending,
                        Priority = RequestPriority.Normal,
                        RequestDate = DateTime.UtcNow,
                        PreferredTime = DateTime.UtcNow,
                        Notes = request.Notes,
                        EstimatedPrice = estimatedPrice
                    };

                    // Handle snake species identification if provided
                    if (request.SnakeSpeciesList != null && request.SnakeSpeciesList.Any())
                    {
                        foreach (var speciesItem in request.SnakeSpeciesList)
                        {
                            // Validate snake species exists
                            var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>().FirstOrDefaultAsync(
                                predicate: s => s.Id == speciesItem.SnakeSpeciesId);

                            if (snakeSpecies == null)
                            {
                                throw new BadRequestException($"Snake species with ID {speciesItem.SnakeSpeciesId} not found.");
                            }

                            // Create CatchingRequestDetail
                            var requestDetail = new CatchingRequestDetail
                            {
                                Id = Guid.NewGuid(),
                                SnakeCatchingRequestId = newRequest.Id,
                                SnakeSpeciesId = speciesItem.SnakeSpeciesId,
                                Quantity = speciesItem.Quantity
                            };

                            await _unitOfWork.GetRepository<CatchingRequestDetail>().InsertAsync(requestDetail);
                        }
                    }

                    // Handle media if provided
                    if (request.MediaIdList != null && request.MediaIdList.Any())
                    {
                        foreach (var mediaId in request.MediaIdList)
                        {
                            var existingMedia = await _unitOfWork.GetRepository<ReportMedia>().FirstOrDefaultAsync(
                                predicate: m => m.Id.ToString() == mediaId);

                            if (existingMedia == null)
                                throw new BadRequestException($"Media with ID {mediaId} not found.");

                            existingMedia.ReferenceId = newRequest.Id;

                            _unitOfWork.GetRepository<ReportMedia>().Update(existingMedia);
                        }
                    }

                    // Save the request
                    await _unitOfWork.GetRepository<SnakeCatchingRequest>().InsertAsync(newRequest);
                    await _unitOfWork.CommitAsync();

                    // Load the created request with navigation properties for response
                    var createdRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == newRequest.Id,
                        include: query => query
                            .Include(r => r.User)
                            .Include(r => r.HandlingOperator)
                            .Include(r => r.Details)
                                .ThenInclude(d => d.SnakeSpecies)
                    );

                    if (createdRequest == null)
                    {
                        throw new Exception("Failed to retrieve created request.");
                    }

                    // Attach media using extension method (handles polymorphic relationship)
                    await createdRequest.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingRequest);

                    _logger.LogInformation(
                        "After AttachReportMediaAsync: Media count = {Count}",
                        createdRequest.Media?.Count ?? 0);

                    var response = createdRequest.Adapt<CreateSnakeCatchingRequestResponse>();

                    // Ensure Media is properly mapped (explicit mapping for polymorphic relationship)
                    if (createdRequest.Media != null && createdRequest.Media.Any())
                    {
                        response.Media = createdRequest.Media.Adapt<List<ReportMediaResponse>>();
                    }

                    // Load AI recognition results from media with SnakeIdentification purpose
                    var aiResults = new List<SnakeDetectionResponse>();
                    if (createdRequest.Media != null && createdRequest.Media.Any())
                    {
                        var identificationMedia = createdRequest.Media.Where(m => m.Purpose == MediaPurpose.SnakeIdentification).ToList();
                        _logger.LogInformation(
                            "Found {Count} media with SnakeIdentification purpose",
                            identificationMedia.Count);

                        foreach (var media in identificationMedia)
                        {
                            _logger.LogInformation(
                                "Processing media {MediaId}, RequiresAI: {RequiresAI}, IsProcessed: {IsProcessed}, AIResults count: {Count}",
                                media.Id, media.RequiresAIProcessing, media.IsProcessed, media.AIRecognitionResults?.Count ?? 0);

                            // Check if this media has completed AI recognition results
                            var completedResults = media.AIRecognitionResults
                                ?.Where(r => r.Status == RecognitionStatus.Completed && r.DetectedSpecies != null)
                                .ToList();

                            if (completedResults != null && completedResults.Any())
                            {
                                _logger.LogInformation(
                                    "Media {MediaId} has {Count} completed AI recognition results",
                                    media.Id, completedResults.Count);

                                foreach (var recognitionResult in completedResults)
                                {
                                    var detectionResponse = new SnakeDetectionResponse
                                    {
                                        Metadata = new AiMetadata
                                        {
                                            ModelVersion = recognitionResult.AIModel?.Version,
                                            ImageWidth = 0,
                                            ImageHeight = 0,
                                            DetectionCount = 1,
                                            Warnings = null
                                        },
                                        RecognitionResultId = recognitionResult.Id,
                                        Results = new List<DetectionResult>
                                        {
                                            new DetectionResult
                                            {
                                                Ai = new AiDetection
                                                {
                                                    ClassId = 0,
                                                    ClassName = recognitionResult.YoloClassName,
                                                    Confidence = (float)recognitionResult.Confidence,
                                                    BBox = new SnakeBBox()
                                                },
                                                Snake = recognitionResult.DetectedSpecies
                                            }
                                        }
                                    };

                                    aiResults.Add(detectionResponse);
                                    _logger.LogInformation(
                                        "Added AI recognition result for media {MediaId}: {SpeciesName}",
                                        media.Id, recognitionResult.DetectedSpecies?.CommonName ?? "Unknown");
                                }
                            }
                            else if (media.RequiresAIProcessing && !media.IsProcessed)
                            {
                                // Media requires AI processing and hasn't been processed yet
                                // Call AI detection synchronously and wait for results
                                try
                                {
                                    _logger.LogInformation(
                                        "Media {MediaId} requires AI processing - calling detection API now",
                                        media.Id);

                                    var detectionResult = await _snakeAIService.DetectFromReportMediaAsync(media.Id);

                                    if (detectionResult != null && detectionResult.Results != null && detectionResult.Results.Any())
                                    {
                                        aiResults.Add(detectionResult);
                                        _logger.LogInformation(
                                            "AI detection completed for media {MediaId}: {SpeciesName}",
                                            media.Id, detectionResult.Results.First().Snake?.CommonName ?? "Unknown");
                                    }
                                    else
                                    {
                                        _logger.LogWarning(
                                            "AI detection returned no results for media {MediaId}",
                                            media.Id);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log the error but don't fail the entire request creation if AI detection fails
                                    _logger.LogError(ex,
                                        "AI detection failed for media {MediaId}: {Message}",
                                        media.Id, ex.Message);
                                }
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Media {MediaId} has no completed AI results. Status of results: {Statuses}",
                                    media.Id,
                                    media.AIRecognitionResults != null
                                        ? string.Join(", ", media.AIRecognitionResults.Select(r => $"{r.Status} (Species: {(r.DetectedSpecies != null ? "Yes" : "No")})"))
                                        : "No results");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No media attached to this request");
                    }

                    // Add AI detection results to response
                    response.AIResults = aiResults;

                    if (response.EstimatedPrice.HasValue)
                    {
                        var perKmRate = _systemSettingService.GetSetting(SystemSettingKeys.LocationIqPricePerKilometer, TRANSFER_PRICE);
                        if (perKmRate > 0)
                        {
                            response.DistanceKm = (double)(response.EstimatedPrice.Value / perKmRate);
                        }
                    }

                    _logger.LogInformation(
                        "Snake catching request created successfully. RequestId: {RequestId}, UserId: {UserId}, Location: ({Lat}, {Lng})",
                        response.Id, userId, request.Lat, request.Lng);

                    return response;
                });

                await _snakeCatchingRequestNotificationService.NotifyRequestCreatedAsync(
                    response.Id,
                    response.UserId,
                    response.Address,
                    response.Lat,
                    response.Lng,
                    response.AdditionalDetails,
                    response.Status,
                    response.EstimatedPrice,
                    response.DistanceKm,
                    response.CreatedAt,
                    response.User?.UserName,
                    response.User?.PhoneNumber);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating snake catching request: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<CreateSnakeCatchingRequestResponse> ConfirmSnakeCatchingRequestAsync(Guid requestId)
        {
            try
            {
                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // fetch and validate inside transaction to ensure data consistency (optimistic locking)
                    var snakeRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: query => query.Include(r => r.User)
                    );

                    if (snakeRequest == null)
                    {
                        throw new NotFoundException("Snake catching request not found.");
                    }

                    // validate status inside transaction (race condition protection)
                    if (snakeRequest.Status != RequestStatus.Pending)
                    {
                        throw new BadRequestException($"Request cannot be accepted. Current status: {snakeRequest.Status}");
                    }


                    // Update the request with pre-calculated price
                    snakeRequest.ConfirmedAt = DateTime.UtcNow;
                    snakeRequest.Status = RequestStatus.Confirmed;

                    _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(snakeRequest);

                    await _unitOfWork.CommitAsync();

                    // Reload the request with all navigation properties for response
                    var updatedRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: query => query
                            .Include(r => r.User)
                            .Include(r => r.HandlingOperator)
                            .Include(r => r.AssignedRescuer)
                                .ThenInclude(ar => ar.Account)
                            .Include(r => r.Missions)
                                .ThenInclude(m => m.CatchingEnvironment)
                            .Include(r => r.Missions)
                                .ThenInclude(m => m.MissionDetails)
                                    .ThenInclude(md => md.SnakeSpecies)
                            .Include(r => r.Details)
                                .ThenInclude(d => d.SnakeSpecies)
                    );

                    if (updatedRequest == null)
                    {
                        throw new Exception("Failed to retrieve updated request.");
                    }

                    await updatedRequest.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingRequest);

                    var response = updatedRequest.Adapt<CreateSnakeCatchingRequestResponse>();
                    if (response.EstimatedPrice.HasValue)
                    {
                        var perKmRate = _systemSettingService.GetSetting(SystemSettingKeys.LocationIqPricePerKilometer, TRANSFER_PRICE);
                        if (perKmRate > 0)
                        {
                            response.DistanceKm = (double)(response.EstimatedPrice.Value / perKmRate);
                        }
                    }

                    //_logger.LogInformation(
                    //    "Snake catching request accepted successfully. RequestId: {RequestId}, RescuerId: {RescuerId}, EstimatedPrice: {Price} VND",
                    //    requestId, rescuerId, estimatedPrice);

                    return response;
                });

                await _snakeCatchingRequestNotificationService.NotifyRequestConfirmedAsync(
                    response.Id,
                    response.UserId,
                    response.Status,
                    response.ConfirmedAt,
                    response.PrePaidAt,
                    response.IsPrePaid);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming snake catching request: {Message}", ex.Message);
                throw;
            }
        }



        public async Task<CreateSnakeCatchingRequestResponse> AssignSnakeCatchingRequestAsync(
            Guid requestId,
            AssignSnakeCatchingRequestRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new BadRequestException("Request data cannot be null.");
                }

                if (request.rescuerId == Guid.Empty)
                {
                    throw new BadRequestException("Rescuer ID is required.");
                }

                // Step 1: Validate rescuer exists and has rescuer profile (OUTSIDE transaction)
                var existingAccount = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                    predicate: a => a.Id == request.rescuerId,
                    include: r => r.Include(i => i.RescuerProfile)
                );

                if (existingAccount == null)
                {
                    throw new NotFoundException("Account not found.");
                }

                if (existingAccount.RescuerProfile == null)
                {
                    throw new BadRequestException("Rescuer profile not found. Only rescuers can accept requests.");
                }

                // Check if rescuer is online
                if (!existingAccount.RescuerProfile.IsOnline && existingAccount.RescuerProfile.IsAvailable)
                {
                    throw new BadRequestException("Rescuer must be online to accept requests.");
                }

                // Step 2: Start transaction for DB operations ONLY
                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // fetch and validate inside transaction to ensure data consistency (optimistic locking)
                    var snakeRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: query => query.Include(r => r.User)
                    );

                    if (snakeRequest == null)
                    {
                        throw new NotFoundException("Snake catching request not found.");
                    }

                    // validate status inside transaction (race condition protection)
                    if (snakeRequest.Status != RequestStatus.Confirmed)
                    {
                        throw new BadRequestException($"Request cannot be accepted. Current status: {snakeRequest.Status}");
                    }

                    // check if request is already assigned (race condition protection)
                    if (snakeRequest.AssignedRescuerId.HasValue)
                    {
                        throw new BadRequestException("This request has already been assigned to another rescuer.");
                    }

                    // Check for existing active mission to prevent duplicate active missions
                    // Aborted missions are kept for history; only block if there's a non-terminal mission
                    var activeMission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.SnakeCatchingRequestId == requestId
                                     && m.Status != CatchingMissionStatus.MissionAborted
                                     && m.Status != CatchingMissionStatus.Cancelled
                    );

                    if (activeMission != null)
                    {
                        throw new BadRequestException($"An active mission already exists for this request with status: {activeMission.Status}");
                    }

                    // Update the request with pre-calculated price
                    snakeRequest.AssignedRescuerId = request.rescuerId;
                    snakeRequest.AssignedAt = DateTime.UtcNow;
                    snakeRequest.Status = RequestStatus.Assigned;

                    _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(snakeRequest);

                    // Create a new mission for this request
                    decimal basePrice = _systemSettingService.GetSetting(SystemSettingKeys.CatchingBasePrice, 500000m);
                    var newMission = new SnakeCatchingMission
                    {
                        Id = Guid.NewGuid(),
                        RescuerId = request.rescuerId,
                        SnakeCatchingRequestId = requestId,
                        Status = CatchingMissionStatus.Preparing,
                        Price = basePrice,
                        EstimatedCost = snakeRequest.EstimatedPrice,
                    };

                    await _unitOfWork.GetRepository<SnakeCatchingMission>().InsertAsync(newMission);
                    await _unitOfWork.CommitAsync();

                    // Reload the request with all navigation properties for response
                    var updatedRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: query => query
                            .Include(r => r.User)
                            .Include(r => r.HandlingOperator)
                            .Include(r => r.AssignedRescuer)
                                .ThenInclude(ar => ar.Account)
                            .Include(r => r.Missions)
                                .ThenInclude(m => m.CatchingEnvironment)
                            .Include(r => r.Missions)
                                .ThenInclude(m => m.MissionDetails)
                                    .ThenInclude(md => md.SnakeSpecies)
                            .Include(r => r.Details)
                                .ThenInclude(d => d.SnakeSpecies)
                    );

                    if (updatedRequest == null)
                    {
                        throw new Exception("Failed to retrieve updated request.");
                    }

                    await updatedRequest.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingRequest);

                    var response = updatedRequest.Adapt<CreateSnakeCatchingRequestResponse>();
                    if (response.EstimatedPrice.HasValue)
                    {
                        var perKmRate = _systemSettingService.GetSetting(SystemSettingKeys.LocationIqPricePerKilometer, TRANSFER_PRICE);
                        if (perKmRate > 0)
                        {
                            response.DistanceKm = (double)(response.EstimatedPrice.Value / perKmRate);
                        }
                    }

                    //_logger.LogInformation(
                    //    "Snake catching request accepted successfully. RequestId: {RequestId}, RescuerId: {RescuerId}, EstimatedPrice: {Price} VND",
                    //    requestId, rescuerId, estimatedPrice);

                    return response;
                });

                await _snakeCatchingRequestNotificationService.NotifyRequestAssignedAsync(
                    response.Id,
                    response.UserId,
                    response.Status,
                    response.AssignedAt,
                    response.AssignedRescuerId,
                    response.AssignedRescuer?.Account?.FullName,
                    response.AssignedRescuer?.PhoneNumber);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting snake catching request: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<DetailSnakeCatchingRequestResponse> GetDetailAsync(Guid requestId)
        {
            try
            {
                // Get the snake catching request with all related data
                var request = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                    predicate: r => r.Id == requestId,
                    include: query => query
                        .Include(r => r.User)
                            .ThenInclude(u => u.Account)
                        .Include(r => r.HandlingOperator)
                        .Include(r => r.AssignedRescuer)
                            .ThenInclude(ar => ar.Account)
                        .Include(r => r.Missions)
                            .ThenInclude(m => m.CatchingEnvironment)
                        .Include(r => r.Missions)
                            .ThenInclude(m => m.MissionDetails)
                                .ThenInclude(md => md.SnakeSpecies)
                        .Include(r => r.Details)
                            .ThenInclude(d => d.SnakeSpecies)
                );

                if (request == null)
                {
                    throw new NotFoundException($"Snake catching request with ID {requestId} not found.");
                }

                // Attach media for request
                await request.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingRequest);

                // Attach media for all missions (historical + active)
                foreach (var mission in request.Missions)
                {
                    await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                }

                // Load AI recognition results for media with SnakeIdentification purpose
                var aiResults = new List<SnakeDetectionResponse>();
                var identificationMedia = await _unitOfWork.GetRepository<ReportMedia>().GetListAsync(
                    predicate: m => m.ReferenceId == requestId &&
                                   m.ReferenceType == MediaReferenceType.SnakeCatchingRequest &&
                                   m.Purpose == MediaPurpose.SnakeIdentification,
                    include: query => query
                        .Include(m => m.AIRecognitionResults)
                            .ThenInclude(r => r.AIModel)
                        .Include(m => m.AIRecognitionResults)
                            .ThenInclude(r => r.DetectedSpecies)
                                .ThenInclude(s => s.SpeciesVenoms)
                                    .ThenInclude(sv => sv.VenomType)
                                        .ThenInclude(v => v.FirstAidGuideline)
                );

                // Build responses from completed recognition results
                foreach (var media in identificationMedia)
                {
                    foreach (var recognitionResult in media.AIRecognitionResults.Where(r => r.Status == RecognitionStatus.Completed))
                    {
                        // Build SnakeDetectionResponse from loaded data
                        var detectionResponse = new SnakeDetectionResponse
                        {
                            Metadata = new AiMetadata
                            {
                                ModelVersion = recognitionResult.AIModel?.Version,
                                ImageWidth = 0,
                                ImageHeight = 0,
                                DetectionCount = 1,
                                Warnings = null
                            },
                            RecognitionResultId = recognitionResult.Id,
                            Results = new List<DetectionResult>
                            {
                                new DetectionResult
                                {
                                    Ai = new AiDetection
                                    {
                                        ClassId = 0,
                                        ClassName = recognitionResult.YoloClassName,
                                        Confidence = (float)recognitionResult.Confidence,
                                        BBox = new SnakeBBox()
                                    },
                                    Snake = recognitionResult.DetectedSpecies
                                }
                            }
                        };

                        aiResults.Add(detectionResponse);
                    }
                }

                var response = request.Adapt<DetailSnakeCatchingRequestResponse>();

                // Ensure Media is properly mapped (explicit mapping for polymorphic relationship)
                if (request.Media != null && request.Media.Any())
                {
                    response.Media = request.Media.Adapt<List<ReportMediaResponse>>();
                }

                response.AIResults = aiResults;
                if (response.EstimatedPrice.HasValue)
                {
                    var perKmRate = _systemSettingService.GetSetting(SystemSettingKeys.LocationIqPricePerKilometer, TRANSFER_PRICE);
                    if (perKmRate > 0)
                    {
                        response.DistanceKm = (double)(response.EstimatedPrice.Value / perKmRate);
                    }
                }

                var additionalSnakePriceValue = _systemSettingService.GetSetting(SystemSettingKeys.CatchingAdditionalSnakePrice, ADDITIONAL_SNAKE_PRICE);

                foreach (var missionResponse in response.Missions)
                {
                    if (missionResponse.MissionDetails == null)
                    {
                        continue;
                    }

                    foreach (var detail in missionResponse.MissionDetails)
                    {
                        detail.Price = detail.Quantity * additionalSnakePriceValue;
                    }
                }

                // Load feedbacks for this specific request
                if (request.AssignedRescuerId.HasValue)
                {
                    var feedbacks = await _unitOfWork.GetRepository<UserFeedback>().GetListAsync(
                        predicate: f => f.TargetUserId == request.AssignedRescuerId.Value &&
                                       f.ReferenceId == requestId &&
                                       f.Type == FeedbackType.Catching,
                        include: query => query
                            .Include(f => f.Rater)
                            .Include(f => f.TargetUser),
                        orderBy: q => q.OrderByDescending(f => f.CreatedAt)
                    );

                    if (feedbacks != null && feedbacks.Any())
                    {
                        foreach (var feedback in feedbacks)
                        {
                            var feedbackResponse = feedback.Adapt<UserFeedbackResponse>();
                            feedbackResponse.RaterName = feedback.Rater?.FullName;
                            feedbackResponse.TargetUserName = feedback.TargetUser?.FullName;
                            response.Feedbacks.Add(feedbackResponse);
                        }
                    }
                }

                _logger.LogInformation(
                    "Snake catching request details retrieved successfully. RequestId: {RequestId}",
                    requestId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting snake catching request detail: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<ListSnakeCatchingRequestResponse>> GetAllRequestAsync(GetAllSnakeCatchingRequestsQuery? query = null)
        {
            try
            {
                query ??= new GetAllSnakeCatchingRequestsQuery();

                var userId = query.UserId;
                var handlingOperatorId = query.HandlingOperatorId;
                var assignedRescuerId = query.AssignedRescuerId;
                var status = query.Status;

                var hasFilter = userId.HasValue || handlingOperatorId.HasValue || assignedRescuerId.HasValue || status.HasValue;

                // Get all snake catching requests with related data
                var requests = await _unitOfWork.GetRepository<SnakeCatchingRequest>().GetListAsync(
                    predicate: r => !hasFilter
                        || (!userId.HasValue || r.UserId == userId.Value)
                        && (!handlingOperatorId.HasValue || r.HandlingOperatorId == handlingOperatorId.Value)
                        && (!assignedRescuerId.HasValue || r.AssignedRescuerId == assignedRescuerId.Value)
                        && (!status.HasValue || r.Status == status.Value),
                    include: query => query
                        .Include(r => r.User)
                            .ThenInclude(u => u.Account)
                        .Include(r => r.AssignedRescuer)
                            .ThenInclude(r => r.Account)
                        .Include(r => r.Details)
                            .ThenInclude(d => d.SnakeSpecies),
                    orderBy: q => q.OrderByDescending(r => r.CreatedAt)
                );

                await requests.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingRequest);

                var response = requests.Adapt<List<ListSnakeCatchingRequestResponse>>();

                _logger.LogInformation(
                    "Retrieved {Count} snake catching requests successfully.",
                    response.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all snake catching requests: {Message}", ex.Message);
                throw;
            }
        }

        private (double centerLng, double centerLat) GetCenterCoordinates()
        {
            var centerLng = _systemSettingService.GetSetting(SystemSettingKeys.PricingCenterLongitude, DEFAULT_CENTER_LONGITUDE);
            var centerLat = _systemSettingService.GetSetting(SystemSettingKeys.PricingCenterLatitude, DEFAULT_CENTER_LATITUDE);

            return (centerLng, centerLat);
        }

        private async Task<decimal> CalculateEstimatedPriceFromCenterAsync(double destinationLng, double destinationLat, string context)
        {
            var (centerLng, centerLat) = GetCenterCoordinates();

            try
            {
                _logger.LogInformation(
                    "Calculating estimated price from center for {Context} - Source: Lng={SourceLng}, Lat={SourceLat} | Dest: Lng={DestLng}, Lat={DestLat}",
                    context,
                    centerLng,
                    centerLat,
                    destinationLng,
                    destinationLat);

                var (distanceInKm, priceInVnd) = await _locationIqService.CalculateDistanceAndPriceAsync(
                    centerLng,
                    centerLat,
                    destinationLng,
                    destinationLat,
                    _systemSettingService.GetSetting(SystemSettingKeys.LocationIqPricePerKilometer, TRANSFER_PRICE));

                _logger.LogInformation(
                    "Estimated price calculated for {Context}: {Distance} km, Price: {Price} VND",
                    context,
                    distanceInKm.ToString("F2"),
                    priceInVnd);

                return priceInVnd;
            }
            catch (ExternalServiceException ex)
            {
                var fallbackPrice = _systemSettingService.GetSetting(SystemSettingKeys.CatchingFallbackEstimatedPrice, 50000m);
                _logger.LogWarning(ex,
                    "Failed to calculate distance from center for {Context}, using fallback price of {FallbackPrice} VND",
                    context,
                    fallbackPrice);
                return fallbackPrice;
            }
        }

        public async Task<DetailSnakeCatchingRequestResponse> CancelSnakeCatchingRequestAsync(
            Guid userId,
            Guid requestId,
            CancelSnakeCatchingRequestRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Reason))
                {
                    throw new BadRequestException("Cancellation reason is required.");
                }

                var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get the snake catching request with missions
                    var snakeCatchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: query => query.Include(r => r.Missions)
                    );

                    if (snakeCatchingRequest == null)
                    {
                        throw new NotFoundException($"Snake catching request with ID {requestId} not found.");
                    }

                    // Validate that the user is the owner of the request
                    if (snakeCatchingRequest.UserId != userId)
                    {
                        throw new BadRequestException("You are not authorized to cancel this request.");
                    }

                    // Handle cancellation based on current status
                    if (snakeCatchingRequest.Status == RequestStatus.Pending)
                    {
                        // If status is Pending, simply change to Cancelled
                        snakeCatchingRequest.Status = RequestStatus.Cancelled;
                        snakeCatchingRequest.CancellationReason = request.Reason;

                        _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(snakeCatchingRequest);

                        _logger.LogInformation(
                            "Snake catching request {RequestId} cancelled from Pending status.",
                            requestId);
                    }
                    else if (snakeCatchingRequest.Status == RequestStatus.Assigned)
                    {
                        // If status is Assigned, check the active mission status
                        var activeMission = snakeCatchingRequest.Missions
                            .OrderByDescending(m => m.CreatedAt)
                            .FirstOrDefault(m => m.Status != CatchingMissionStatus.MissionAborted
                                              && m.Status != CatchingMissionStatus.Cancelled);

                        if (activeMission == null)
                        {
                            throw new BadRequestException("Active mission not found for this assigned request.");
                        }

                        if (activeMission.Status == CatchingMissionStatus.Preparing)
                        {
                            // If mission status is Preparing, allow cancellation
                            snakeCatchingRequest.Status = RequestStatus.Cancelled;
                            snakeCatchingRequest.CancellationReason = request.Reason;

                            // Update mission status to Cancelled
                            activeMission.Status = CatchingMissionStatus.Cancelled;
                            activeMission.CancellationReason = request.Reason;

                            _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(snakeCatchingRequest);
                            _unitOfWork.GetRepository<SnakeCatchingMission>().Update(activeMission);

                            _logger.LogInformation(
                                "Snake catching request {RequestId} and mission {MissionId} cancelled from Assigned/Preparing status.",
                                requestId, activeMission.Id);
                        }
                        else if (activeMission.Status == CatchingMissionStatus.EnRoute)
                        {
                            // If mission status is EnRoute, do not allow cancellation
                            throw new BadRequestException("Cannot cancel request. The rescuer is already on the way (En Route).");
                        }
                        else
                        {
                            // Other mission statuses (Arrived, Completed, etc.)
                            throw new BadRequestException($"Cannot cancel request. Mission status is {activeMission.Status}.");
                        }
                    }
                    else
                    {
                        // Other request statuses (Finished, Paid, Completed, Cancelled, etc.)
                        throw new BadRequestException($"Cannot cancel request with status {snakeCatchingRequest.Status}.");
                    }

                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Snake catching request {RequestId} cancelled. Status updated to Cancelled.",
                        requestId);

                    // Reload the request with all navigation properties for response
                    var updatedRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: query => query
                            .Include(r => r.User)
                                .ThenInclude(u => u.Account)
                            .Include(r => r.HandlingOperator)
                            .Include(r => r.AssignedRescuer)
                                .ThenInclude(ar => ar.Account)
                            .Include(r => r.Missions)
                                .ThenInclude(m => m.CatchingEnvironment)
                            .Include(r => r.Missions)
                                .ThenInclude(m => m.MissionDetails)
                                    .ThenInclude(md => md.SnakeSpecies)
                            .Include(r => r.Details)
                                .ThenInclude(d => d.SnakeSpecies)
                    );

                    if (updatedRequest == null)
                    {
                        throw new Exception("Failed to retrieve updated request.");
                    }

                    // Attach media for request
                    await updatedRequest.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingRequest);

                    // Attach media for all missions
                    foreach (var mission in updatedRequest.Missions)
                    {
                        await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                    }

                    var response = updatedRequest.Adapt<DetailSnakeCatchingRequestResponse>();
                    if (response.EstimatedPrice.HasValue)
                    {
                        var perKmRate = _systemSettingService.GetSetting(SystemSettingKeys.LocationIqPricePerKilometer, TRANSFER_PRICE);
                        if (perKmRate > 0)
                        {
                            response.DistanceKm = (double)(response.EstimatedPrice.Value / perKmRate);
                        }
                    }

                    _logger.LogInformation(
                        "Snake catching request cancelled successfully. RequestId: {RequestId}, UserId: {UserId}, Reason: {Reason}",
                        requestId, userId, request.Reason);

                    return response;
                });

                await _snakeCatchingRequestNotificationService.NotifyRequestCancelledAsync(
                    response.Id,
                    response.UserId,
                    response.Status,
                    response.CancellationReason,
                    response.AssignedRescuerId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling snake catching request: {Message}", ex.Message);
                throw;
            }
        }


        public async Task<PagedData<OperatorSnakeCatchingRequestSummaryResponse>> GetActiveRequestsAsync(
            IEnumerable<RequestStatus>? statuses,
            DateTimeOffset? since,
            DateTimeOffset? until,
            int page,
            int pageSize)
        {
            try
            {
                var defaultStatuses = new[]
                {
                    RequestStatus.Pending,
                    RequestStatus.Confirmed,
                    RequestStatus.Assigned,
                };

                var effectiveStatuses = (statuses != null && statuses.Any())
                    ? statuses
                    : defaultStatuses;

                var repo = _unitOfWork.GetRepository<SnakeCatchingRequest>();
                var pagedResult = await repo.GetPagingListAsync<OperatorSnakeCatchingRequestSummaryResponse>(
                    predicate: r =>
                        effectiveStatuses.Contains(r.Status) &&
                        (!since.HasValue || r.CreatedAt >= since.Value.UtcDateTime) &&
                        (!until.HasValue || r.CreatedAt <= until.Value.UtcDateTime),
                    orderBy: q => q.OrderByDescending(r => r.CreatedAt),
                    page: page,
                    size: pageSize,
                    selector: r => new OperatorSnakeCatchingRequestSummaryResponse
                    {
                        Id = r.Id,
                        Status = r.Status,
                        LocationCoordinates = new GeoPointResponse
                        {
                            Latitude = r.LocationCoordinates.Y,
                            Longitude = r.LocationCoordinates.X
                        },
                        Address = r.Address,
                        RequestDate = r.RequestDate,
                        AssignedRescuerId = r.AssignedRescuerId,
                        HandlingOperatorId = r.HandlingOperatorId,
                        Priority = r.Priority
                    });

                return pagedResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active snake catching requests: {Message}", ex.Message);
                throw;
            }
        }

        private string? ExtractFilenameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segments = uri.Segments;
                return segments.Length > 0 ? segments[^1] : null;
            }
            catch
            {
                return null;
            }
        }

        private string DetermineContentType(string url)
        {
            var extension = System.IO.Path.GetExtension(url).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                _ => "application/octet-stream"
            };
        }
    }
}
