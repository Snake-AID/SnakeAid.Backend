using System.Security.Claims;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Media;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

/// <summary>
/// Service implementation for media management operations
/// </summary>
public class MediaService : IMediaService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        ICloudinaryService cloudinaryService,
        ILogger<MediaService> logger)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReportMediaResponse> UploadReportMediaAsync(
        UploadReportMediaRequest request,
        MediaReferenceType referenceType,
        MediaPurpose purpose,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Uploading report media for reference {ReferenceId}, type: {Type}, purpose: {Purpose}",
            request.ReferenceId, referenceType, purpose);

        // Upload file to Cloudinary first
        var uploadResult = await _cloudinaryService.UploadImageAsync(request.File, user, "report-media", ct);

        // Save media record to database
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var reportMedia = new ReportMedia
            {
                Id = Guid.NewGuid(),
                FileName = request.File.FileName,
                MediaUrl = uploadResult.SecureUrl,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                ReferenceId = request.ReferenceId,
                ReferenceType = referenceType,
                Purpose = purpose,
                RequiresAIProcessing = purpose == MediaPurpose.SnakeIdentification,
                IsProcessed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var repository = _unitOfWork.GetRepository<ReportMedia>();
            await repository.InsertAsync(reportMedia);


            await _unitOfWork.CommitAsync();

            var response = new ReportMediaResponse
            {
                Id = reportMedia.Id,
                MediaUrl = reportMedia.MediaUrl,
                FileName = reportMedia.FileName,
                ContentType = reportMedia.ContentType,
                FileSize = reportMedia.FileSize,
                ReferenceType = reportMedia.ReferenceType,
                Purpose = reportMedia.Purpose,
                RequiresAIProcessing = reportMedia.RequiresAIProcessing
            };

            _logger.LogInformation("Successfully uploaded report media with ID: {MediaId}", reportMedia.Id);
            return response;
        });
    }
}