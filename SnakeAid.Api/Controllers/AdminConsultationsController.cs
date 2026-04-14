using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/consultations")]
public class AdminConsultationsController : BaseController<AdminConsultationsController>
{
    private readonly IConsultationService _consultationService;

    public AdminConsultationsController(
        IConsultationService consultationService,
        ILogger<AdminConsultationsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper)
        : base(logger, httpContextAccessor, mapper)
    {
        _consultationService = consultationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagingResponse<AdminConsultationResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagingResponse<AdminConsultationResponse>>>> GetAllConsultations(
        [FromQuery] AdminConsultationsQueryRequest query)
    {
        var result = await _consultationService.GetAllConsultationsForAdminAsync(query);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpGet("{consultationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminConsultationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminConsultationResponse>>> GetConsultationById(Guid consultationId)
    {
        var result = await _consultationService.GetConsultationByIdForAdminAsync(consultationId);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }
}
