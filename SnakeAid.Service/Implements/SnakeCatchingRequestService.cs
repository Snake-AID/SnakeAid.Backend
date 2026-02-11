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
    }
}
