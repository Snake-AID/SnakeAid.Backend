using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Requests.SnakeCatchingRequest;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.SnakeCatchingRequest;
using SnakeAid.Core.Responses.SnakeDetection;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Extensions;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class SnakeCatchingRequestService : ISnakeCatchingRequestService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakeCatchingRequestService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILocationIqService _locationIqService;
        private readonly IPayOsPaymentService _payOsPaymentService;
        private readonly ISnakeAIService _snakeAIService;
        private readonly decimal additionalSnakePrice = 100000;

        public SnakeCatchingRequestService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakeCatchingRequestService> logger,
            IConfiguration configuration,
            ILocationIqService locationIqService,
            IPayOsPaymentService payOsPaymentService,
            ISnakeAIService snakeAIService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _locationIqService = locationIqService;
            _payOsPaymentService = payOsPaymentService;
            _snakeAIService = snakeAIService;
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

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
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
                        Notes = request.Notes
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

                    var aiResults = new List<SnakeDetectionResponse>();

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

                            if (existingMedia.Purpose == MediaPurpose.SnakeIdentification)
                            {
                                // If media is for snake identification, call SnakeAIService to identify species
                                try
                                {
                                    var identifiedSpecies = await _snakeAIService.DetectFromReportMediaAsync(existingMedia.Id);
                                    if (identifiedSpecies != null)
                                    {
                                        _logger.LogInformation(
                                            "Snake species identified by AI for media {MediaId}: {SpeciesName}",
                                            existingMedia.Id, identifiedSpecies.Results.First().Snake.CommonName);
                                        aiResults.Add(identifiedSpecies);
                                    }
                                    else
                                    {
                                        _logger.LogWarning(
                                            "Snake AI service could not identify species for media {MediaId}",
                                            existingMedia.Id);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log the error but don't fail the entire request creation if AI detection fails
                                    _logger.LogError(ex, "Error calling Snake AI service for media {MediaId}: {Message}", existingMedia.Id, ex.Message);
                                }
                            }
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
                            .Include(r => r.Details)
                                .ThenInclude(d => d.SnakeSpecies)
                    );

                    if (createdRequest == null)
                    {
                        throw new Exception("Failed to retrieve created request.");
                    }

                    // Attach media using extension method (handles polymorphic relationship)
                    await createdRequest.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingRequest);

                    var response = createdRequest.Adapt<CreateSnakeCatchingRequestResponse>();

                    // Ensure Media is properly mapped (explicit mapping for polymorphic relationship)
                    if (createdRequest.Media != null && createdRequest.Media.Any())
                    {
                        response.Media = createdRequest.Media.Adapt<List<ReportMediaResponse>>();
                    }

                    // Add AI detection results to response
                    response.AIResults = aiResults;

                    if (response.EstimatedPrice.HasValue)
                    {
                        var perKmRate = _configuration.GetValue<decimal>("LocationIq:PricePerKilometer");
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating snake catching request: {Message}", ex.Message);
                throw;
            }
        }



        public async Task<CreateSnakeCatchingRequestResponse> AcceptSnakeCatchingRequestAsync(
            Guid rescuerId,
            Guid requestId,
            AcceptSnakeCatchingRequestRequest request)
        {
            try
            {
                // Step 1: Validate rescuer exists and has rescuer profile (OUTSIDE transaction)
                var existingAccount = await _unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(
                    predicate: a => a.Id == rescuerId,
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
                if (!existingAccount.RescuerProfile.IsOnline)
                {
                    throw new BadRequestException("Rescuer must be online to accept requests.");
                }

                // Step 2: Get snake catching request to retrieve coordinates (OUTSIDE transaction)
                var snakeRequestForCoords = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                    predicate: r => r.Id == requestId,
                    include: query => query.Include(r => r.User)
                );

                if (snakeRequestForCoords == null)
                {
                    throw new NotFoundException("Snake catching request not found.");
                }

                // Pre-validate request status
                if (snakeRequestForCoords.Status != RequestStatus.Pending)
                {
                    throw new BadRequestException($"Request cannot be accepted. Current status: {snakeRequestForCoords.Status}");
                }

                // Check if request is already assigned
                if (snakeRequestForCoords.AssignedRescuerId.HasValue)
                {
                    throw new BadRequestException("This request has already been assigned to another rescuer.");
                }

                // Step 3: Calculate distance and price BEFORE transaction (external HTTP call)
                decimal estimatedPrice;
                try
                {
                    // Log coordinates for debugging
                    _logger.LogInformation(
                        "Calculating distance - Rescuer (Source): Lng={SourceLng}, Lat={SourceLat} | " +
                        "Request (Dest): Lng={DestLng}, Lat={DestLat} (from Point.X/Y)",
                        request.Lng, request.Lat,
                        snakeRequestForCoords.LocationCoordinates.X,
                        snakeRequestForCoords.LocationCoordinates.Y);

                    var (distanceInKm, priceInVnd) = await _locationIqService.CalculateDistanceAndPriceAsync(
                        request.Lng,
                        request.Lat,
                        snakeRequestForCoords.LocationCoordinates.X, // Longitude
                        snakeRequestForCoords.LocationCoordinates.Y  // Latitude
                    );

                    estimatedPrice = priceInVnd;

                    _logger.LogInformation(
                        "Distance calculated for RequestId {RequestId}: {Distance} km, Price: {Price} VND",
                        requestId, distanceInKm.ToString("F2"), estimatedPrice);
                }
                catch (ExternalServiceException ex)
                {
                    _logger.LogWarning(ex, "Failed to calculate distance, using default price of 50000 VND");
                    estimatedPrice = 50000; // Fallback price
                }

                // Step 4: Start transaction for DB operations ONLY
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Re-fetch and re-validate inside transaction to ensure data consistency (optimistic locking)
                    var snakeRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: query => query.Include(r => r.User)
                    );

                    if (snakeRequest == null)
                    {
                        throw new NotFoundException("Snake catching request not found.");
                    }

                    // Re-validate status inside transaction (race condition protection)
                    if (snakeRequest.Status != RequestStatus.Pending)
                    {
                        throw new BadRequestException($"Request cannot be accepted. Current status: {snakeRequest.Status}");
                    }

                    // Re-check if request is already assigned (race condition protection)
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
                    snakeRequest.AssignedRescuerId = rescuerId;
                    snakeRequest.AssignedAt = DateTime.UtcNow;
                    snakeRequest.Status = RequestStatus.Assigned;
                    snakeRequest.EstimatedPrice = estimatedPrice;

                    _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(snakeRequest);

                    // Create a new mission for this request
                    var newMission = new SnakeCatchingMission
                    {
                        Id = Guid.NewGuid(),
                        RescuerId = rescuerId,
                        SnakeCatchingRequestId = requestId,
                        Status = CatchingMissionStatus.Preparing,
                        Price = estimatedPrice,
                        EstimatedCost = estimatedPrice
                    };

                    await _unitOfWork.GetRepository<SnakeCatchingMission>().InsertAsync(newMission);
                    await _unitOfWork.CommitAsync();

                    // Reload the request with all navigation properties for response
                    var updatedRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: query => query
                            .Include(r => r.User)
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
                        var perKmRate = _configuration.GetValue<decimal>("LocationIq:PricePerKilometer");
                        if (perKmRate > 0)
                        {
                            response.DistanceKm = (double)(response.EstimatedPrice.Value / perKmRate);
                        }
                    }

                    _logger.LogInformation(
                        "Snake catching request accepted successfully. RequestId: {RequestId}, RescuerId: {RescuerId}, EstimatedPrice: {Price} VND",
                        requestId, rescuerId, estimatedPrice);

                    return response;
                });
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
                    var perKmRate = _configuration.GetValue<decimal>("LocationIq:PricePerKilometer");
                    if (perKmRate > 0)
                    {
                        response.DistanceKm = (double)(response.EstimatedPrice.Value / perKmRate);
                    }
                }

                foreach (var missionResponse in response.Missions)
                {
                    foreach (var detail in missionResponse.MissionDetails)
                    {
                        detail.Price = detail.Quantity * additionalSnakePrice;
                    }
                }

                // Load feedbacks for assigned rescuer if exists
                if (request.AssignedRescuerId.HasValue)
                {
                    var feedbacks = await _unitOfWork.GetRepository<UserFeedback>().GetListAsync(
                        predicate: f => f.TargetUserId == request.AssignedRescuerId.Value,
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

        public async Task<List<ListSnakeCatchingRequestResponse>> GetAllRequestAsync()
        {
            try
            {
                // Get all snake catching requests with related data
                var requests = await _unitOfWork.GetRepository<SnakeCatchingRequest>().GetListAsync(
                    include: query => query
                        .Include(r => r.User)
                            .ThenInclude(u => u.Account)
                        .Include(r => r.AssignedRescuer)
                            .ThenInclude(r => r.Account)
                        .Include(r => r.Details)
                            .ThenInclude(d => d.SnakeSpecies),
                    orderBy: q => q.OrderByDescending(r => r.RequestDate)
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

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
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
                        var perKmRate = _configuration.GetValue<decimal>("LocationIq:PricePerKilometer");
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling snake catching request: {Message}", ex.Message);
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
