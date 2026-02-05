using System.Security.Claims;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Requests.Media;
using SnakeAid.Core.Responses.Media;

namespace SnakeAid.Service.Interfaces;

/// <summary>
/// Service interface for media management operations
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Upload and save report media to database
    /// </summary>
    /// <param name="request">Upload request with file and reference ID</param>
    /// <param name="referenceType">Type of reference entity</param>
    /// <param name="purpose">Purpose of the media</param>
    /// <param name="user">Current user claims principal</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with media details</returns>
    Task<ReportMediaResponse> UploadReportMediaAsync(
        UploadReportMediaRequest request,
        MediaReferenceType referenceType,
        MediaPurpose purpose,
        ClaimsPrincipal user,
        CancellationToken ct = default);
}