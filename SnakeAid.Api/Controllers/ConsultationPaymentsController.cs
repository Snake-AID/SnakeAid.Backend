using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/consultation-payments")]
[Authorize]
public class ConsultationPaymentsController : BaseController<ConsultationPaymentsController>
{
    private readonly IConsultationPaymentService _consultationPaymentService;

    public ConsultationPaymentsController(
        ILogger<ConsultationPaymentsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IConsultationPaymentService consultationPaymentService)
        : base(logger, httpContextAccessor, mapper)
    {
        _consultationPaymentService = consultationPaymentService;
    }

    [HttpPost("/api/consultation-bookings/{bookingId:guid}/payments")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<ApiResponse<ConsultationPaymentResponse>>> PayScheduledBooking(
        Guid bookingId,
        [FromBody] ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _consultationPaymentService.PayScheduledBookingAsync(userId, bookingId, request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPost("/api/consultations/emergency-requests/{requestId:guid}/payments")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<ApiResponse<ConsultationPaymentResponse>>> PayEmergencyRequest(
        Guid requestId,
        [FromBody] ProcessConsultationPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _consultationPaymentService.PayEmergencyRequestAsync(userId, requestId, request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }
}
