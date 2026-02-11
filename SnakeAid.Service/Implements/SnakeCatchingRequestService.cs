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

namespace SnakeAid.Service.Implements
{
    public class SnakeCatchingRequestService : ISnakeCatchingRequestService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakeCatchingRequestService> _logger;
        private readonly IConfiguration _configuration;

        public SnakeCatchingRequestService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakeCatchingRequestService> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
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

                    // Validate RequestDate
                    if (request.RequestDate < DateTime.UtcNow.AddMinutes(-5))
                    {
                        throw new BadRequestException("RequestDate cannot be in the past (more than 5 minutes ago).");
                    }

                    // Validate PreferredTime if provided
                    if (request.PreferredTime.HasValue && request.PreferredTime.Value < DateTime.UtcNow)
                    {
                        throw new BadRequestException("PreferredTime cannot be in the past.");
                    }

                    // Validate EstimatedPrice
                    if (request.EstimatedPrice.HasValue && request.EstimatedPrice.Value <= 0)
                    {
                        throw new BadRequestException("EstimatedPrice must be greater than 0.");
                    }

                    // Create Point from lng/lat (PostGIS uses SRID 4326 - WGS84)
                    var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                    var locationPoint = geometryFactory.CreatePoint(
                        new NetTopologySuite.Geometries.Coordinate(request.Lng, request.Lat));

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
                        RequestDate = request.RequestDate.ToUniversalTime(),
                        PreferredTime = request.PreferredTime?.ToUniversalTime(),
                        EstimatedPrice = request.EstimatedPrice,
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
                                Purpose = MediaPurpose.SnakeIdentification,
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
                            .Include(r => r.Media)
                            .Include(r => r.Details)
                                .ThenInclude(d => d.SnakeSpecies)
                    );

                    if (createdRequest == null)
                    {
                        throw new Exception("Failed to retrieve created request.");
                    }

                    var response = createdRequest.Adapt<CreateSnakeCatchingRequestResponse>();
                    
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

        public async Task<CreateSnakeCatchingRequestResponse> AcceptSnakeCatchingRequestAsync(
            Guid rescuerId,
            Guid requestId)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Validate rescuer exists and has rescuer profile
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

                    // Get the snake catching request
                    var request = await _unitOfWork.GetRepository<SnakeCatchingRequest>().FirstOrDefaultAsync(
                        predicate: r => r.Id == requestId,
                        include: query => query
                            .Include(r => r.User)
                            .Include(r => r.Media)
                    );

                    if (request == null)
                    {
                        throw new NotFoundException("Snake catching request not found.");
                    }

                    // Validate request status
                    if (request.Status != RequestStatus.Pending)
                    {
                        throw new BadRequestException($"Request cannot be accepted. Current status: {request.Status}");
                    }

                    // Check if request is already assigned
                    if (request.AssignedRescuerId.HasValue)
                    {
                        throw new BadRequestException("This request has already been assigned to another rescuer.");
                    }

                    // Update the request
                    request.AssignedRescuerId = rescuerId;
                    request.AssignedAt = DateTime.UtcNow;
                    request.Status = RequestStatus.Assigned;

                    _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(request);

                    // Create a new mission for this request
                    var newMission = new SnakeCatchingMission
                    {
                        Id = Guid.NewGuid(),
                        RescuerId = rescuerId,
                        SnakeCatchingRequestId = requestId,
                        Status = CatchingMissionStatus.Preparing,
                        Price = request.EstimatedPrice ?? 0, // Use estimated price or 0
                        EstimatedCost = request.EstimatedPrice
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
                            .Include(r => r.Media)
                            .Include(r => r.Mission)
                            .Include(r => r.Details)
                                .ThenInclude(d => d.SnakeSpecies)
                    );

                    if (updatedRequest == null)
                    {
                        throw new Exception("Failed to retrieve updated request.");
                    }

                    var response = updatedRequest.Adapt<CreateSnakeCatchingRequestResponse>();

                    _logger.LogInformation(
                        "Snake catching request accepted successfully. RequestId: {RequestId}, RescuerId: {RescuerId}",
                        requestId, rescuerId);

                    return response;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting snake catching request: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<CreateSnakeCatchingRequestResponse> GetDetailAsync(Guid requestId)
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
                        .Include(r => r.Media)
                        .Include(r => r.Mission)
                        .Include(r => r.Details)
                            .ThenInclude(d => d.SnakeSpecies)
                );

                if (request == null)
                {
                    throw new NotFoundException($"Snake catching request with ID {requestId} not found.");
                }

                var response = request.Adapt<CreateSnakeCatchingRequestResponse>();

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
    }
}
