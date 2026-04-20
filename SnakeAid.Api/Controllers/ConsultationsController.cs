using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/consultations")]
[Authorize]
public class ConsultationsController : BaseController<ConsultationsController>
{
    private readonly IConsultationService _consultationService;

    public ConsultationsController(
        ILogger<ConsultationsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IConsultationService consultationService)
        : base(logger, httpContextAccessor, mapper)
    {
        _consultationService = consultationService;
    }

    [HttpPost("{consultationId:guid}/end")]
    public async Task<IActionResult> EndConsultation(Guid consultationId)
    {
        var actorId = GetCurrentUserId();
        await _consultationService.EndConsultationAsync(consultationId, actorId);
        return Ok(ApiResponseBuilder.BuildSuccessResponse("Consultation ended successfully."));
    }

    [HttpGet("/api/users/me/consultations")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<ApiResponse<PagingResponse<MyConsultationResponse>>>> GetMyConsultations([FromQuery] MyConsultationsQueryRequest query)
    {
        var userId = GetCurrentUserId();
        var result = await _consultationService.GetMyConsultationsAsync(userId, query);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPost("{consultationId:guid}/expert-absent-report")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(typeof(ApiResponse<MyConsultationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MyConsultationResponse>>> ReportExpertAbsent(
        Guid consultationId,
        [FromBody] ReportExpertAbsentRequest request)
    {
        var memberId = GetCurrentUserId();
        var result = await _consultationService.ReportExpertAbsentAsync(consultationId, memberId, request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPost("{consultationId:guid}/reviews")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<ApiResponse<UserFeedbackResponse>>> CreateReview(Guid consultationId, [FromBody] CreateConsultationReviewRequest request)
    {
        var raterId = GetCurrentUserId();
        var result = await _consultationService.CreateConsultationReviewAsync(consultationId, raterId, request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpGet("{consultationId:guid}/reviews")]
    public async Task<IActionResult> GetReview(Guid consultationId)
    {
        var actorId = GetCurrentUserId();
        var result = await _consultationService.GetConsultationReviewAsync(consultationId, actorId);
        if (result == null)
            return Ok(ApiResponseBuilder.BuildSuccessResponse<UserFeedbackResponse?>(null, "No review found for this consultation."));
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }
}
