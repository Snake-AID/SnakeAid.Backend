using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Responses.SnakeDetection;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

/// <summary>
/// Snake Detection endpoints for frontend
/// </summary>
[ApiController]
[Route("api/detection")]
[Authorize]
public class SnakeDetectionController : BaseController<SnakeDetectionController>
{
    private readonly ISnakeAIService _snakeAIService;

    public SnakeDetectionController(
        ILogger<SnakeDetectionController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ISnakeAIService snakeAIService)
        : base(logger, httpContextAccessor, mapper)
    {
        _snakeAIService = snakeAIService;
    }

    /// <summary>
    /// Detect snake species from uploaded ReportMedia
    /// </summary>
    /// <param name="request">Detection request with ReportMediaId</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detection result with species info and confidence</returns>
    [HttpPost("detect/{reportMediaId:guid}")]
    [SwaggerOperation(
        Summary = "Detect snake from ReportMedia",
        Description = "Analyze image from ReportMedia using SnakeAI model to detect snake species and map to SnakeLibs")]
    [SwaggerResponse(200, "Detection successful", typeof(ApiResponse<SnakeDetectionResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "ReportMedia not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(503, "AI service unavailable", typeof(ApiResponse<object>))]
    public async Task<IActionResult> Detect([FromRoute] Guid reportMediaId, CancellationToken ct = default)
    {
        var result = await _snakeAIService.DetectFromReportMediaAsync(reportMediaId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get detection result by ID
    /// </summary>
    /// <param name="id">Recognition result ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detection result details</returns>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get detection result",
        Description = "Retrieve saved detection result by ID")]
    [SwaggerResponse(200, "Detection result found", typeof(ApiResponse<SnakeDetectionResponse>))]
    [SwaggerResponse(404, "Detection result not found", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetDetectionResult(Guid id, CancellationToken ct = default)
    {
        var result = await _snakeAIService.GetRecognitionResultAsync(id, ct);
        return StatusCode(result.StatusCode, result);
    }
}
