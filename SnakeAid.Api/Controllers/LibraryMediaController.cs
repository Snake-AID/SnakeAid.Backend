using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.LibraryMedia;
using SnakeAid.Core.Responses.LibraryMedia;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/library-media")]
public class LibraryMediaController : BaseController<LibraryMediaController>
{
    private readonly ILibraryMediaService _libraryMediaService;

    public LibraryMediaController(
        ILogger<LibraryMediaController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ILibraryMediaService libraryMediaService)
        : base(logger, httpContextAccessor, mapper)
    {
        _libraryMediaService = libraryMediaService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ValidateFile(maxSizeInMB: 100, allowedExtensions: new[] { ".jpg", ".jpeg", ".png", ".webp", ".mp4", ".mov", ".avi", ".pdf", ".doc", ".docx", ".txt" }, formFieldName: "file")]
    [SwaggerOperation(Summary = "Create library media", Description = "Upload file to Cloudinary and create LibraryMedia record")]
    [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<LibraryMediaResponse>))]
    public async Task<IActionResult> Create([FromForm] CreateLibraryMediaRequest request, CancellationToken ct)
    {
        var result = await _libraryMediaService.CreateAsync(request, User, ct);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Library media created successfully."));
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get library media by ID")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<LibraryMediaResponse>))]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _libraryMediaService.GetByIdAsync(id, ct);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Filter library media", Description = "Get paginated library media with filters")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedData<LibraryMediaResponse>>))]
    public async Task<IActionResult> GetPaged([FromQuery] GetLibraryMediaRequest request, CancellationToken ct)
    {
        var result = await _libraryMediaService.GetPagedAsync(request, ct);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Update library media", Description = "Update metadata and optionally replace file on Cloudinary")]
    [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<LibraryMediaResponse>))]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateLibraryMediaRequest request, CancellationToken ct)
    {
        var result = await _libraryMediaService.UpdateAsync(id, request, User, ct);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Library media updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete library media", Description = "Delete LibraryMedia record and remove Cloudinary file")]
    [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<object>))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _libraryMediaService.DeleteAsync(id, ct);
        return Ok(ApiResponseBuilder.BuildSuccessResponse("Library media deleted successfully."));
    }
}