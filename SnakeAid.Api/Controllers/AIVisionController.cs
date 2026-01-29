using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Requests.AIVision;
using SnakeAid.Core.Responses.AIVision;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

/// <summary>
/// AI Vision endpoints for snake detection
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIVisionController : ControllerBase
{
    private readonly ISnakeAIService _snakeAIService;
    private readonly ILogger<AIVisionController> _logger;

    public AIVisionController(ISnakeAIService snakeAIService, ILogger<AIVisionController> logger)
    {
        _snakeAIService = snakeAIService;
        _logger = logger;
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
    [SwaggerResponse(200, "Detection successful", typeof(AIVisionDetectResponse))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(503, "AI service unavailable")]
    public async Task<IActionResult> Detect([FromBody] AIVisionDetectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return BadRequest(new { error = "ImageUrl is required" });
        }

        // Check if AI service is available
        if (!await _snakeAIService.IsHealthyAsync())
        {
            _logger.LogWarning("SnakeAI service is unavailable");
            return StatusCode(503, new
            {
                error = "AI Service Unavailable",
                message = "Snake detection service is currently unavailable. Please try again later."
            });
        }

        try
        {
            var result = await _snakeAIService.DetectAsync(request.ImageUrl);

            var topDetection = result.Detections
                .OrderByDescending(d => d.Confidence)
                .FirstOrDefault();

            var response = new AIVisionDetectResponse
            {
                ModelVersion = result.ModelVersion,
                ImageWidth = result.ImageWidth,
                ImageHeight = result.ImageHeight,
                TopClassName = topDetection?.ClassName,
                TopConfidence = topDetection?.Confidence,
                DetectionCount = result.Detections.Count,
                Detections = result.Detections,
                Warnings = result.Warnings
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detection failed for URL: {Url}", request.ImageUrl);
            return StatusCode(500, new
            {
                error = "Detection Failed",
                message = "An error occurred while processing the image."
            });
        }
    }
}
