using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Media;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/media")]
public class MediaController : BaseController<MediaController>
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IMediaService _mediaService;

    public MediaController(
        ILogger<MediaController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ICloudinaryService cloudinaryService,
        IMediaService mediaService)
        : base(logger, httpContextAccessor, mapper)
    {
        _cloudinaryService = cloudinaryService;
        _mediaService = mediaService;
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [ValidateFile(maxSizeInMB: 5, allowedExtensions: new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }, formFieldName: "file")]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request, CancellationToken ct)
    {
        var result = await _cloudinaryService.UploadImageAsync(request.File, User, request.Domain, ct);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Image uploaded successfully."));
    }

    [HttpPost("upload-file")]
    [Consumes("multipart/form-data")]
    [ValidateFile(maxSizeInMB: 100, allowedExtensions: new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".txt", ".doc", ".docx" }, formFieldName: "file")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest request, CancellationToken ct)
    {
        var result = await _cloudinaryService.UploadFileAsync(request.File, User, request.Domain, ct);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "File uploaded successfully."));
    }

    /// <summary>
    /// Upload media for reports (Community Report, Snakebite Incident, etc.)
    /// </summary>
    /// <param name="type">Reference type (CommunityReport, SnakebiteIncident, etc.)</param>
    /// <param name="purpose">Media purpose (default: SnakeIdentification)</param>
    /// <param name="request">Upload request with file and reference ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>ReportMedia with ID and URL for detection</returns>
    [HttpPost("report")]
    [Consumes("multipart/form-data")]
    [ValidateFile(maxSizeInMB: 5, allowedExtensions: new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }, formFieldName: "file")]
    [SwaggerOperation(
        Summary = "Upload report media",
        Description = "Upload image for reports and create ReportMedia entity for AI detection")]
    [SwaggerResponse(200, "Media uploaded successfully", typeof(ApiResponse<ReportMediaResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    public async Task<IActionResult> UploadReportMedia(
        [FromQuery] MediaReferenceType type,
        [FromForm] UploadReportMediaRequest request,
        [FromQuery] MediaPurpose purpose = MediaPurpose.SnakeIdentification,
        CancellationToken ct = default)
    {
        var result = await _mediaService.UploadReportMediaAsync(request, type, purpose, User, ct);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Report media uploaded successfully."));
    }
}
