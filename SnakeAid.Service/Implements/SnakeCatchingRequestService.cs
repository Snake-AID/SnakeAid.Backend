using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.SnakeCatchingRequest;
using SnakeAid.Core.Responses.SnakeCatchingRequest;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Extensions;

namespace SnakeAid.Service.Implements
{
    public class SnakeCatchingRequestService : ISnakeCatchingRequestService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakeCatchingRequestService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILocationIqService _locationIqService;

        public SnakeCatchingRequestService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakeCatchingRequestService> logger,
            IConfiguration configuration,
            ILocationIqService locationIqService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _locationIqService = locationIqService;
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

                    // Handle media if provided
                    if (request.MediaURLList != null && request.MediaURLList.Any())
                    {
                        var uploadBatchId = Guid.NewGuid();
                        var sequenceOrder = 0;

                        foreach (var mediaUrl in request.MediaURLList)
                        {
                            if (string.IsNullOrWhiteSpace(mediaUrl))
                            {
                                continue; // Skip empty URLs
                            }

                            // Extract filename from URL or generate one
                            var filename = ExtractFilenameFromUrl(mediaUrl) ?? $"snake_catching_media_{DateTime.UtcNow:yyyyMMddHHmmss}_{sequenceOrder}";

                            var media = new ReportMedia
                            {
                                Id = Guid.NewGuid(),
                                ReferenceId = newRequest.Id,
                                ReferenceType = MediaReferenceType.SnakeCatchingRequest,
                                FileName = filename,
                                MediaUrl = mediaUrl,
                                ContentType = DetermineContentType(mediaUrl),
                                FileSize = 0, // Will be updated later if needed
                                Purpose = MediaPurpose.SnakeOthers,
                                UploadBatchId = uploadBatchId,
                                SequenceOrder = sequenceOrder++,
                                RequiresAIProcessing = true,
                                IsProcessed = false
                            };

                            await _unitOfWork.GetRepository<ReportMedia>().InsertAsync(media);
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

                    await createdRequest.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingRequest);

                    var response = createdRequest.Adapt<CreateSnakeCatchingRequestResponse>();
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
                            .Include(r => r.Mission)
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
                        .Include(r => r.Mission)
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
                
                // Attach media for mission if exists
                if (request.Mission != null)
                {
                    await request.Mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                }

                var response = request.Adapt<DetailSnakeCatchingRequestResponse>();
                if (response.EstimatedPrice.HasValue)
                {
                    var perKmRate = _configuration.GetValue<decimal>("LocationIq:PricePerKilometer");
                    if (perKmRate > 0)
                    {
                        response.DistanceKm = (double)(response.EstimatedPrice.Value / perKmRate);
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
