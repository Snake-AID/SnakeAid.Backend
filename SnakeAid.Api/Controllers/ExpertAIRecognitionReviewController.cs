using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.AIRecognition;
using SnakeAid.Core.Responses.AIRecognition;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/experts/ai-recognition")]
[Authorize(Roles = "Expert")]
public class ExpertAIRecognitionReviewController : BaseController<ExpertAIRecognitionReviewController>
{
    private readonly IAIRecognitionReportMediaService _reportMediaService;

    public ExpertAIRecognitionReviewController(
        ILogger<ExpertAIRecognitionReviewController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IAIRecognitionReportMediaService reportMediaService)
        : base(logger, httpContextAccessor, mapper)
    {
        _reportMediaService = reportMediaService;
    }

    [HttpGet("review-queue")]
    public async Task<IActionResult> GetReviewQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportMediaService.GetExpertReviewQueueAsync(
            page,
            pageSize,
            cancellationToken);

        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Expert review queue retrieved."));
    }

    [HttpGet("review-queue/{recognitionResultId:guid}")]
    public async Task<IActionResult> GetReviewQueueDetail(
        [FromRoute] Guid recognitionResultId,
        CancellationToken cancellationToken = default)
    {
        var expertId = GetCurrentUserId();
        var result = await _reportMediaService.GetExpertReviewQueueDetailAsync(recognitionResultId, expertId, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Expert review item detail retrieved."));
    }

    [HttpGet("review-history")]
    public async Task<IActionResult> GetReviewHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var expertId = GetCurrentUserId();
        var result = await _reportMediaService.GetExpertReviewHistoryAsync(expertId, page, pageSize, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Expert review history retrieved."));
    }

    [HttpPost("review-queue/{recognitionResultId:guid}/verify")]
    [ValidateModel]
    public async Task<IActionResult> Verify(
        [FromRoute] Guid recognitionResultId,
        [FromBody] ExpertVerifyRecognitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var expertId = GetCurrentUserId();
        var result = await _reportMediaService.VerifyRecognitionAsync(recognitionResultId, expertId, request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Recognition result verified by expert."));
    }

    [HttpPost("review-queue/{recognitionResultId:guid}/reject")]
    [ValidateModel]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid recognitionResultId,
        [FromBody] ExpertRejectRecognitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var expertId = GetCurrentUserId();
        var result = await _reportMediaService.RejectRecognitionAsync(recognitionResultId, expertId, request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Recognition result rejected by expert."));
    }
}
