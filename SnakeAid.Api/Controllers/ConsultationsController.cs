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
[Route("api/v1/consultations")]
[Authorize]
public class ConsultationsController : BaseController<ConsultationsController>
{
    private readonly IConsultationService _consultationService;
    private readonly IEmergencyConsultationService _emergencyConsultationService;

    public ConsultationsController(
        ILogger<ConsultationsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IConsultationService consultationService,
        IEmergencyConsultationService emergencyConsultationService)
        : base(logger, httpContextAccessor, mapper)
    {
        _consultationService = consultationService;
        _emergencyConsultationService = emergencyConsultationService;
    }

    [HttpPost("{consultationId:guid}/end")]
    public async Task<IActionResult> EndConsultation(Guid consultationId)
    {
        var actorId = GetCurrentUserId();
        await _consultationService.EndConsultationAsync(consultationId, actorId);
        return Ok(ApiResponseBuilder.BuildSuccessResponse("Consultation ended successfully."));
    }

    [HttpPost("{consultationId:guid}/reviews")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<ApiResponse<UserFeedbackResponse>>> CreateReview(Guid consultationId, [FromBody] CreateConsultationReviewRequest request)
    {
        var raterId = GetCurrentUserId();
        var result = await _consultationService.CreateConsultationReviewAsync(consultationId, raterId, request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPost("emergency")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<ApiResponse<EmergencyConsultationRequestResponse>>> CreateEmergencyConsultationRequest([FromBody] CreateEmergencyConsultationRequest request)
    {
        var requesterId = GetCurrentUserId();
        var result = await _emergencyConsultationService.CreateEmergencyRequestAsync(requesterId, request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPost("emergency-requests/{requestId:guid}/accept")]
    [Authorize(Roles = "Expert")]
    public async Task<ActionResult<ApiResponse<EmergencyConsultationRequestResponse>>> AcceptEmergencyConsultationRequest(Guid requestId)
    {
        var expertId = GetCurrentUserId();
        var result = await _emergencyConsultationService.AcceptEmergencyRequestAsync(requestId, expertId);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPost("emergency-requests/{requestId:guid}/reject")]
    [Authorize(Roles = "Expert")]
    public async Task<ActionResult<ApiResponse<EmergencyConsultationRequestResponse>>> RejectEmergencyConsultationRequest(Guid requestId)
    {
        var expertId = GetCurrentUserId();
        var result = await _emergencyConsultationService.RejectEmergencyRequestAsync(requestId, expertId);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }
}
