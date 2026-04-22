using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.ExpertCertificate;
using SnakeAid.Core.Responses.ExpertCertificate;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class ExpertCertificateService : IExpertCertificateService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly ILogger<ExpertCertificateService> _logger;

    public ExpertCertificateService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        ILogger<ExpertCertificateService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExpertCertificateResponse> CreateMyAsync(
        Guid expertId,
        CreateExpertCertificateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCertificateDates(request.IssueDate, request.ExpiryDate);
        var profile = await RequireExpertProfileAsync(expertId, false, cancellationToken);
        var media = await ValidateAndLoadMediaAsync(request.ReportMediaIds, cancellationToken);

        var certificate = new ExpertCertificate
        {
            Id = Guid.NewGuid(),
            ExpertId = profile.AccountId,
            CertificateName = request.CertificateName.Trim(),
            IssuingOrganization = request.IssuingOrganization.Trim(),
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            VerificationStatus = VerificationStatus.Pending,
            RejectionReason = string.Empty
        };

        await _unitOfWork.GetRepository<ExpertCertificate>().InsertAsync(certificate, cancellationToken);
        await SyncCertificateMediaAsync(certificate, media, cancellationToken);
        await RecalculateExpertVerificationAsync(profile.AccountId, cancellationToken);
        await _unitOfWork.CommitAsync();

        await LoadMediaAsync(certificate, cancellationToken);
        _logger.LogInformation("Expert {ExpertId} created certificate {CertificateId}.", expertId, certificate.Id);
        return MapResponse(certificate);
    }

    public async Task<IReadOnlyList<ExpertCertificateResponse>> GetMyListAsync(
        Guid expertId,
        CancellationToken cancellationToken = default)
    {
        await RequireExpertProfileAsync(expertId, true, cancellationToken);
        var items = (await _unitOfWork.GetRepository<ExpertCertificate>().GetListAsync(
                predicate: c => c.ExpertId == expertId,
                orderBy: q => q.OrderByDescending(c => c.UpdatedAt),
                asNoTracking: true,
                cancellationToken: cancellationToken))
            .ToList();

        await LoadMediaAsync(items, cancellationToken);
        return items.Select(MapResponse).ToList();
    }

    public async Task<ExpertCertificateResponse> GetMyDetailAsync(
        Guid expertId,
        Guid certificateId,
        CancellationToken cancellationToken = default)
    {
        var certificate = await RequireOwnedCertificateAsync(expertId, certificateId, true, cancellationToken);
        await LoadMediaAsync(certificate, cancellationToken);
        return MapResponse(certificate);
    }

    public async Task<ExpertCertificateResponse> UpdateMyAsync(
        Guid expertId,
        Guid certificateId,
        UpdateExpertCertificateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCertificateDates(request.IssueDate, request.ExpiryDate);
        var certificate = await RequireOwnedCertificateAsync(expertId, certificateId, false, cancellationToken);
        var media = await ValidateAndLoadMediaAsync(request.ReportMediaIds, cancellationToken);

        certificate.CertificateName = request.CertificateName.Trim();
        certificate.IssuingOrganization = request.IssuingOrganization.Trim();
        certificate.IssueDate = request.IssueDate;
        certificate.ExpiryDate = request.ExpiryDate;
        certificate.VerificationStatus = VerificationStatus.Pending;
        certificate.RejectionReason = string.Empty;

        await SyncCertificateMediaAsync(certificate, media, cancellationToken);
        await RecalculateExpertVerificationAsync(expertId, cancellationToken);
        await _unitOfWork.CommitAsync();

        await LoadMediaAsync(certificate, cancellationToken);
        _logger.LogInformation("Expert {ExpertId} updated certificate {CertificateId} and reset it to pending.", expertId, certificateId);
        return MapResponse(certificate);
    }

    public async Task DeleteMyAsync(
        Guid expertId,
        Guid certificateId,
        CancellationToken cancellationToken = default)
    {
        var certificate = await RequireOwnedCertificateAsync(expertId, certificateId, false, cancellationToken);
        await DetachCertificateMediaAsync(certificate.Id, cancellationToken);
        _unitOfWork.GetRepository<ExpertCertificate>().Delete(certificate);
        await RecalculateExpertVerificationAsync(expertId, cancellationToken);
        await _unitOfWork.CommitAsync();
    }

    public async Task<ExpertCertificateResponse> AdminCreateAsync(
        AdminCreateExpertCertificateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCertificateDates(request.IssueDate, request.ExpiryDate);
        ValidateReviewState(request.VerificationStatus, request.RejectionReason);
        var profile = await RequireExpertProfileAsync(request.ExpertId, false, cancellationToken);
        var media = await ValidateAndLoadMediaAsync(request.ReportMediaIds, cancellationToken);

        var certificate = new ExpertCertificate
        {
            Id = Guid.NewGuid(),
            ExpertId = profile.AccountId,
            CertificateName = request.CertificateName.Trim(),
            IssuingOrganization = request.IssuingOrganization.Trim(),
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            VerificationStatus = request.VerificationStatus,
            RejectionReason = request.VerificationStatus == VerificationStatus.Rejected
                ? request.RejectionReason!.Trim()
                : string.Empty
        };

        await _unitOfWork.GetRepository<ExpertCertificate>().InsertAsync(certificate, cancellationToken);
        await SyncCertificateMediaAsync(certificate, media, cancellationToken);
        await RecalculateExpertVerificationAsync(profile.AccountId, cancellationToken);
        await _unitOfWork.CommitAsync();

        await LoadMediaAsync(certificate, cancellationToken);
        _logger.LogInformation("Admin created certificate {CertificateId} for expert {ExpertId} with status {Status}.",
            certificate.Id,
            profile.AccountId,
            certificate.VerificationStatus);
        return MapResponse(certificate);
    }

    public async Task<PagedData<ExpertCertificateResponse>> AdminGetListAsync(
        AdminExpertCertificateQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.GetRepository<ExpertCertificate>()
            .CreateBaseQuery()
            .OrderByDescending(c => c.UpdatedAt)
            .AsQueryable();

        if (request.ExpertId.HasValue)
        {
            query = query.Where(c => c.ExpertId == request.ExpertId.Value);
        }

        if (request.VerificationStatus.HasValue)
        {
            query = query.Where(c => c.VerificationStatus == request.VerificationStatus.Value);
        }

        var paged = await query.ToPaginatedResponse(request.PageNumber, request.PageSize);
        var items = paged.Items.ToList();
        await LoadMediaAsync(items, cancellationToken);

        return new PagedData<ExpertCertificateResponse>
        {
            Items = items.Select(MapResponse).ToList(),
            Meta = paged.Meta
        };
    }

    public async Task<ExpertCertificateResponse> AdminGetDetailAsync(
        Guid certificateId,
        CancellationToken cancellationToken = default)
    {
        var certificate = await RequireCertificateAsync(certificateId, true, cancellationToken);
        await LoadMediaAsync(certificate, cancellationToken);
        return MapResponse(certificate);
    }

    public async Task<ExpertCertificateResponse> AdminUpdateAsync(
        Guid certificateId,
        AdminUpdateExpertCertificateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCertificateDates(request.IssueDate, request.ExpiryDate);
        ValidateReviewState(request.VerificationStatus, request.RejectionReason);

        var certificate = await RequireCertificateAsync(certificateId, false, cancellationToken);
        var media = await ValidateAndLoadMediaAsync(request.ReportMediaIds, cancellationToken);

        certificate.CertificateName = request.CertificateName.Trim();
        certificate.IssuingOrganization = request.IssuingOrganization.Trim();
        certificate.IssueDate = request.IssueDate;
        certificate.ExpiryDate = request.ExpiryDate;
        certificate.VerificationStatus = request.VerificationStatus;
        certificate.RejectionReason = request.VerificationStatus == VerificationStatus.Rejected
            ? request.RejectionReason!.Trim()
            : string.Empty;

        await SyncCertificateMediaAsync(certificate, media, cancellationToken);
        await RecalculateExpertVerificationAsync(certificate.ExpertId, cancellationToken);
        await _unitOfWork.CommitAsync();

        await LoadMediaAsync(certificate, cancellationToken);
        return MapResponse(certificate);
    }

    public async Task DeleteAdminAsync(
        Guid certificateId,
        CancellationToken cancellationToken = default)
    {
        var certificate = await RequireCertificateAsync(certificateId, false, cancellationToken);
        await DetachCertificateMediaAsync(certificate.Id, cancellationToken);
        _unitOfWork.GetRepository<ExpertCertificate>().Delete(certificate);
        await RecalculateExpertVerificationAsync(certificate.ExpertId, cancellationToken);
        await _unitOfWork.CommitAsync();
    }

    private async Task<ExpertProfile> RequireExpertProfileAsync(Guid expertId, bool asNoTracking, CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
            predicate: p => p.AccountId == expertId,
            include: q => q.Include(p => p.Account),
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);

        if (profile?.Account == null || profile.Account.Role != AccountRole.Expert)
        {
            throw new NotFoundException($"Expert profile {expertId} not found.");
        }

        return profile;
    }

    private async Task<ExpertCertificate> RequireOwnedCertificateAsync(Guid expertId, Guid certificateId, bool asNoTracking, CancellationToken cancellationToken)
    {
        var certificate = await _unitOfWork.GetRepository<ExpertCertificate>().FirstOrDefaultAsync(
            predicate: c => c.Id == certificateId && c.ExpertId == expertId,
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);

        if (certificate == null)
        {
            throw new NotFoundException("Expert certificate not found.");
        }

        return certificate;
    }

    private async Task<ExpertCertificate> RequireCertificateAsync(Guid certificateId, bool asNoTracking, CancellationToken cancellationToken)
    {
        var certificate = await _unitOfWork.GetRepository<ExpertCertificate>().FirstOrDefaultAsync(
            predicate: c => c.Id == certificateId,
            asNoTracking: asNoTracking,
            cancellationToken: cancellationToken);

        if (certificate == null)
        {
            throw new NotFoundException("Expert certificate not found.");
        }

        return certificate;
    }

    private async Task<List<ReportMedia>> ValidateAndLoadMediaAsync(IEnumerable<Guid> requestedMediaIds, CancellationToken cancellationToken)
    {
        var mediaIds = requestedMediaIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (mediaIds.Count == 0)
        {
            throw new ValidationException("At least one report media ID is required.");
        }

        var media = (await _unitOfWork.GetRepository<ReportMedia>().GetListAsync(
                predicate: m => mediaIds.Contains(m.Id),
                asNoTracking: false,
                cancellationToken: cancellationToken))
            .ToList();

        if (media.Count != mediaIds.Count)
        {
            throw new NotFoundException("One or more report media items were not found.");
        }

        foreach (var item in media)
        {
            if (item.ReferenceType != MediaReferenceType.ExpertCertificate)
            {
                throw new ConflictException($"Report media {item.Id} is not uploaded as ExpertCertificate media.");
            }
        }

        return media;
    }

    private async Task SyncCertificateMediaAsync(ExpertCertificate certificate, List<ReportMedia> requestedMedia, CancellationToken cancellationToken)
    {
        var mediaRepo = _unitOfWork.GetRepository<ReportMedia>();
        var existingMedia = (await mediaRepo.GetListAsync(
                predicate: m => m.ReferenceId == certificate.Id && m.ReferenceType == MediaReferenceType.ExpertCertificate,
                asNoTracking: false,
                cancellationToken: cancellationToken))
            .ToList();

        foreach (var media in requestedMedia)
        {
            if (media.ReferenceId.HasValue && media.ReferenceId.Value != certificate.Id)
            {
                throw new ConflictException($"Report media {media.Id} is already attached to another certificate.");
            }
        }

        var requestedIds = requestedMedia.Select(m => m.Id).ToHashSet();

        foreach (var media in existingMedia.Where(m => !requestedIds.Contains(m.Id)))
        {
            media.ReferenceId = null;
            media.SequenceOrder = null;
            mediaRepo.Update(media);
        }

        var sequence = 1;
        foreach (var media in requestedMedia.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id))
        {
            media.ReferenceId = certificate.Id;
            media.ReferenceType = MediaReferenceType.ExpertCertificate;
            media.SequenceOrder = sequence++;
            mediaRepo.Update(media);
        }

        certificate.CertificateUrl = requestedMedia
            .OrderBy(m => m.SequenceOrder ?? int.MaxValue)
            .ThenBy(m => m.CreatedAt)
            .Select(m => m.MediaUrl)
            .FirstOrDefault() ?? string.Empty;
    }

    private async Task DetachCertificateMediaAsync(Guid certificateId, CancellationToken cancellationToken)
    {
        var mediaRepo = _unitOfWork.GetRepository<ReportMedia>();
        var media = await mediaRepo.GetListAsync(
            predicate: m => m.ReferenceId == certificateId && m.ReferenceType == MediaReferenceType.ExpertCertificate,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        foreach (var item in media)
        {
            item.ReferenceId = null;
            item.SequenceOrder = null;
            mediaRepo.Update(item);
        }
    }

    private Task LoadMediaAsync(ExpertCertificate certificate, CancellationToken cancellationToken)
    {
        return LoadMediaAsync(new[] { certificate }, cancellationToken);
    }

    private async Task LoadMediaAsync(IEnumerable<ExpertCertificate> certificates, CancellationToken cancellationToken)
    {
        var certificateList = certificates.ToList();
        if (certificateList.Count == 0)
        {
            return;
        }

        var certificateIds = certificateList.Select(c => c.Id).ToList();
        var media = await _unitOfWork.GetRepository<ReportMedia>().GetListAsync(
            predicate: m => m.ReferenceId.HasValue
                && certificateIds.Contains(m.ReferenceId.Value)
                && m.ReferenceType == MediaReferenceType.ExpertCertificate,
            orderBy: q => q.OrderBy(m => m.SequenceOrder).ThenBy(m => m.CreatedAt),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        var grouped = media
            .Where(m => m.ReferenceId.HasValue)
            .GroupBy(m => m.ReferenceId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var certificate in certificateList)
        {
            certificate.Media = grouped.GetValueOrDefault(certificate.Id) ?? new List<ReportMedia>();
        }
    }

    private async Task RecalculateExpertVerificationAsync(Guid expertId, CancellationToken cancellationToken)
    {
        var profile = await RequireExpertProfileAsync(expertId, false, cancellationToken);
        var persistedCertificates = (await _unitOfWork.GetRepository<ExpertCertificate>().GetListAsync(
                predicate: c => c.ExpertId == expertId,
                asNoTracking: false,
                cancellationToken: cancellationToken))
            .ToDictionary(c => c.Id);

        foreach (var entry in _unitOfWork.Context.ChangeTracker.Entries<ExpertCertificate>()
                     .Where(e => e.Entity.ExpertId == expertId))
        {
            if (entry.State == EntityState.Deleted)
            {
                persistedCertificates.Remove(entry.Entity.Id);
                continue;
            }

            persistedCertificates[entry.Entity.Id] = entry.Entity;
        }

        var statuses = persistedCertificates.Values
            .Select(c => c.VerificationStatus)
            .ToList();

        profile.IsVerified = statuses.Count > 0 && statuses.All(s => s == VerificationStatus.Verified);
    }

    private static ExpertCertificateResponse MapResponse(ExpertCertificate certificate)
    {
        return new ExpertCertificateResponse
        {
            Id = certificate.Id,
            ExpertId = certificate.ExpertId,
            CertificateName = certificate.CertificateName,
            IssuingOrganization = certificate.IssuingOrganization,
            IssueDate = certificate.IssueDate,
            ExpiryDate = certificate.ExpiryDate,
            CertificateUrl = certificate.CertificateUrl,
            Media = certificate.Media
                .OrderBy(m => m.SequenceOrder ?? int.MaxValue)
                .ThenBy(m => m.CreatedAt)
                .Select(m => new ReportMediaResponse
                {
                    Id = m.Id,
                    MediaUrl = m.MediaUrl,
                    FileName = m.FileName,
                    ContentType = m.ContentType,
                    FileSize = m.FileSize,
                    ReferenceType = m.ReferenceType,
                    Purpose = m.Purpose,
                    RequiresAIProcessing = m.RequiresAIProcessing
                })
                .ToList(),
            VerificationStatus = certificate.VerificationStatus,
            RejectionReason = certificate.RejectionReason
        };
    }

    private static void ValidateCertificateDates(DateTime issueDate, DateTime? expiryDate)
    {
        if (expiryDate.HasValue && expiryDate.Value < issueDate)
        {
            throw new ValidationException("ExpiryDate cannot be earlier than IssueDate.");
        }
    }

    private static void ValidateReviewState(VerificationStatus status, string? rejectionReason)
    {
        if (status == VerificationStatus.Rejected && string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new ValidationException("RejectionReason is required when verification status is Rejected.");
        }
    }
}
