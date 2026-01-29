using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Media;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/media")]
public class MediaController : BaseController<MediaController>
{
    private readonly ICloudinaryService _cloudinaryService;

    public MediaController(
        ILogger<MediaController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ICloudinaryService cloudinaryService)
        : base(logger, httpContextAccessor, mapper)
    {
        _cloudinaryService = cloudinaryService;
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [ValidateFile(maxSizeInMB: 10, allowedExtensions: new[] { ".jpg", ".jpeg", ".png", ".webp" }, formFieldName: "file")]
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
}
