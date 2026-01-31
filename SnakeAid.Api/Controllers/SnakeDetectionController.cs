using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SnakeDetection;
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
    /// Detect snake species from image URL
    /// </summary>
    /// <param name="request">Detection request with image URL</param>
    /// <returns>Detection result with species and confidence</returns>
    [HttpPost("detect")]
    [SwaggerOperation(
        Summary = "Detect snake from image",
        Description = "Analyze image from Cloudinary URL using SnakeAI model to detect snake species")]
    [SwaggerResponse(200, "Detection successful", typeof(ApiResponse<SnakeDetectionResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(503, "AI service unavailable", typeof(ApiResponse<object>))]
    public async Task<IActionResult> Detect([FromBody] SnakeDetectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            var errorResponse = ApiResponseBuilder.BuildErrorResponse(
                "ImageUrl is required",
                "INVALID_REQUEST");
            return StatusCode(errorResponse.StatusCode, errorResponse);
        }

        // Check if AI service is available
        if (!await _snakeAIService.IsHealthyAsync())
        {
            _logger.LogWarning("SnakeAI service is unavailable");
            var unavailableResponse = ApiResponseBuilder.BuildErrorResponse(
                "Snake detection service is currently unavailable. Please try again later.",
                "SERVICE_UNAVAILABLE",
                null,
                System.Net.HttpStatusCode.ServiceUnavailable);
            return StatusCode(unavailableResponse.StatusCode, unavailableResponse);
        }

        var result = await _snakeAIService.DetectAsync(request.ImageUrl);
        return StatusCode(result.StatusCode, result);
    }
}
