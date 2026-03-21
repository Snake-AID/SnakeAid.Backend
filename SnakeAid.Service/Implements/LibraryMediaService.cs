using System.Security.Claims;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.LibraryMedia;
using SnakeAid.Core.Responses.LibraryMedia;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class LibraryMediaService : ILibraryMediaService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<LibraryMediaService> _logger;

    public LibraryMediaService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        ICloudinaryService cloudinaryService,
        ILogger<LibraryMediaService> logger)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    public async Task<LibraryMediaResponse> CreateAsync(CreateLibraryMediaRequest request, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (request == null)
        {
            throw new BadRequestException("Request data cannot be null.");
        }

        if (request.SnakeSpeciesId.HasValue)
        {
            var snakeSpeciesExists = await _unitOfWork.GetRepository<SnakeSpecies>()
                .ExistsAsync(x => x.Id == request.SnakeSpeciesId.Value, ct);

            if (!snakeSpeciesExists)
            {
                throw new NotFoundException($"Snake species with ID {request.SnakeSpeciesId.Value} not found.");
            }
        }

        var uploadResult = await UploadByMediaTypeAsync(request.File, request.MediaType, user, ct);
        var uploadedById = GetUserId(user);

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = new LibraryMedia
            {
                Id = Guid.NewGuid(),
                MediaUrl = uploadResult.SecureUrl,
                MediaType = request.MediaType,
                FileName = request.File.FileName,
                FileSizeBytes = request.File.Length,
                ContentType = request.File.ContentType,
                IsActive = true,
                IsPublic = true,
                SnakeSpeciesId = request.SnakeSpeciesId,
                UploadedById = uploadedById,
                UploadedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<LibraryMedia>().InsertAsync(entity, ct);
            await _unitOfWork.CommitAsync();

            return entity.Adapt<LibraryMediaResponse>();
        });
    }

    public async Task<LibraryMediaResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _unitOfWork.GetRepository<LibraryMedia>()
            .FirstOrDefaultAsync(predicate: x => x.Id == id, include: q => q.Include(x => x.SnakeSpecies), cancellationToken: ct);

        if (entity == null)
        {
            throw new NotFoundException($"Library media with ID {id} not found.");
        }

        return entity.Adapt<LibraryMediaResponse>();
    }

    public async Task<PagedData<LibraryMediaResponse>> GetPagedAsync(GetLibraryMediaRequest request, CancellationToken ct = default)
    {
        var fileNameKeyword = request.FileName?.Trim();

        var pagedData = await _unitOfWork.GetRepository<LibraryMedia>()
            .GetPagingListAsync(
                predicate: x =>
                    (!request.SnakeSpeciesId.HasValue || x.SnakeSpeciesId == request.SnakeSpeciesId.Value)
                    && (!request.MediaType.HasValue || x.MediaType == request.MediaType.Value)
                    && (!request.IsActive.HasValue || x.IsActive == request.IsActive.Value)
                    && (!request.IsPublic.HasValue || x.IsPublic == request.IsPublic.Value)
                    && (string.IsNullOrWhiteSpace(fileNameKeyword) || (x.FileName != null && x.FileName.Contains(fileNameKeyword))),
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                include: q => q.Include(x => x.SnakeSpecies),
                page: request.PageNumber,
                size: request.PageSize,
                cancellationToken: ct);

        return new PagedData<LibraryMediaResponse>
        {
            Items = pagedData.Items.Adapt<List<LibraryMediaResponse>>(),
            Meta = pagedData.Meta
        };
    }

    public async Task<LibraryMediaResponse> UpdateAsync(Guid id, UpdateLibraryMediaRequest request, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (request == null)
        {
            throw new BadRequestException("Request data cannot be null.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var repository = _unitOfWork.GetRepository<LibraryMedia>();
            var entity = await repository.FirstOrDefaultAsync(predicate: x => x.Id == id, asNoTracking: false, cancellationToken: ct);

            if (entity == null)
            {
                throw new NotFoundException($"Library media with ID {id} not found.");
            }

            if (request.SnakeSpeciesId.HasValue)
            {
                var snakeSpeciesExists = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .ExistsAsync(x => x.Id == request.SnakeSpeciesId.Value, ct);

                if (!snakeSpeciesExists)
                {
                    throw new NotFoundException($"Snake species with ID {request.SnakeSpeciesId.Value} not found.");
                }

                entity.SnakeSpeciesId = request.SnakeSpeciesId;
            }

            var oldMediaUrl = entity.MediaUrl;

            if (request.File != null && request.File.Length > 0)
            {
                var effectiveMediaType = request.MediaType ?? entity.MediaType;
                var uploadResult = await UploadByMediaTypeAsync(request.File, effectiveMediaType, user, ct);

                entity.MediaUrl = uploadResult.SecureUrl;
                entity.FileName = request.File.FileName;
                entity.FileSizeBytes = request.File.Length;
                entity.ContentType = request.File.ContentType;
                entity.MediaType = effectiveMediaType;
                entity.UploadedAt = DateTime.UtcNow;
                entity.UploadedById = GetUserId(user);
            }
            else if (request.MediaType.HasValue)
            {
                entity.MediaType = request.MediaType.Value;
            }

            if (request.IsActive.HasValue)
            {
                entity.IsActive = request.IsActive.Value;
            }

            if (request.IsPublic.HasValue)
            {
                entity.IsPublic = request.IsPublic.Value;
            }

            entity.UpdatedAt = DateTime.UtcNow;

            repository.Update(entity);
            await _unitOfWork.CommitAsync();

            if (request.File != null && request.File.Length > 0 && !string.IsNullOrWhiteSpace(oldMediaUrl) && !string.Equals(oldMediaUrl, entity.MediaUrl, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _cloudinaryService.DeleteByUrlAsync(oldMediaUrl, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old Cloudinary asset for LibraryMedia {LibraryMediaId}", entity.Id);
                }
            }

            return entity.Adapt<LibraryMediaResponse>();
        });
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var repository = _unitOfWork.GetRepository<LibraryMedia>();
            var entity = await repository.FirstOrDefaultAsync(predicate: x => x.Id == id, asNoTracking: false, cancellationToken: ct);

            if (entity == null)
            {
                throw new NotFoundException($"Library media with ID {id} not found.");
            }

            if (!string.IsNullOrWhiteSpace(entity.MediaUrl))
            {
                try
                {
                    await _cloudinaryService.DeleteByUrlAsync(entity.MediaUrl, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete Cloudinary asset for LibraryMedia {LibraryMediaId}", entity.Id);
                }
            }

            repository.Delete(entity);
            await _unitOfWork.CommitAsync();
        });
    }

    private Task<Core.Responses.Media.CloudinaryUploadResult> UploadByMediaTypeAsync(IFormFile file, MediaType mediaType, ClaimsPrincipal user, CancellationToken ct)
    {
        return mediaType == MediaType.Image
            ? _cloudinaryService.UploadImageAsync(file, user, "library-media", ct)
            : _cloudinaryService.UploadFileAsync(file, user, "library-media", ct);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("User ID not found in token.");
        }

        return userId;
    }
}