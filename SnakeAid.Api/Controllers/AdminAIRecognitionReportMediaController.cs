using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/admin/ai-recognition-report-media")]
[Authorize(Roles = "Admin")]
public class AdminAIRecognitionReportMediaController : BaseController<AdminAIRecognitionReportMediaController>
{
    private readonly IAIRecognitionReportMediaService _reportMediaService;

    public AdminAIRecognitionReportMediaController(
        ILogger<AdminAIRecognitionReportMediaController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IAIRecognitionReportMediaService reportMediaService)
        : base(logger, httpContextAccessor, mapper)
    {
        _reportMediaService = reportMediaService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] RecognitionStatus? status = null,
        [FromQuery] decimal? minConfidence = null,
        [FromQuery] decimal? maxConfidence = null,
        [FromQuery] MediaReferenceType? referenceType = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportMediaService.GetAdminReportMediaListAsync(
            page,
            pageSize,
            status,
            minConfidence,
            maxConfidence,
            referenceType,
            from,
            to,
            cancellationToken);

        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "AI recognition report media list retrieved."));
    }

    [HttpGet("{recognitionResultId:guid}")]
    public async Task<IActionResult> GetDetail(
        [FromRoute] Guid recognitionResultId,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportMediaService.GetAdminReportMediaDetailAsync(recognitionResultId, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "AI recognition report media detail retrieved."));
    }
}
