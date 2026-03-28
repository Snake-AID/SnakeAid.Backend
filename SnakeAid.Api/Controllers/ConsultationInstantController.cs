using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/consultations/instant")]
[Authorize]
public class ConsultationInstantController : BaseController<ConsultationInstantController>
{
    private readonly IEmergencyConsultationService _emergencyConsultationService;

    public ConsultationInstantController(
        ILogger<ConsultationInstantController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IEmergencyConsultationService emergencyConsultationService)
        : base(logger, httpContextAccessor, mapper)
    {
        _emergencyConsultationService = emergencyConsultationService;
    }

    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<ApiResponse<EmergencyConsultationRequestResponse>>> CreateEmergencyConsultationRequest([FromBody] CreateEmergencyConsultationRequest request)
    {
        var requesterId = GetCurrentUserId();
        var result = await _emergencyConsultationService.CreateEmergencyRequestAsync(requesterId, request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPost("{requestId:guid}/accept")]
    [Authorize(Roles = "Expert")]
    public async Task<ActionResult<ApiResponse<EmergencyConsultationRequestResponse>>> AcceptEmergencyConsultationRequest(Guid requestId)
    {
        var expertId = GetCurrentUserId();
        var result = await _emergencyConsultationService.AcceptEmergencyRequestAsync(requestId, expertId);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPost("{requestId:guid}/reject")]
    [Authorize(Roles = "Expert")]
    public async Task<ActionResult<ApiResponse<EmergencyConsultationRequestResponse>>> RejectEmergencyConsultationRequest(Guid requestId)
    {
        var expertId = GetCurrentUserId();
        var result = await _emergencyConsultationService.RejectEmergencyRequestAsync(requestId, expertId);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }
}
