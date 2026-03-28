using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/consultations/scheduled")]
[Authorize]
public class ConsultationBookingsController : BaseController<ConsultationBookingsController>
{
    private readonly IBookingService _bookingService;

    public ConsultationBookingsController(
        ILogger<ConsultationBookingsController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IBookingService bookingService)
        : base(logger, httpContextAccessor, mapper)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<ApiResponse<ConsultationBookingResponse>>> CreateBooking([FromBody] CreateConsultationBookingRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _bookingService.CreateScheduledBookingAsync(userId, request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpGet("/api/users/me/consultations/scheduled")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ConsultationBookingResponse>>>> GetMyBookings()
    {
        var userId = GetCurrentUserId();
        var result = await _bookingService.GetMyBookingsAsync(userId);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpGet("/api/experts/me/consultations/scheduled")]
    [Authorize(Roles = "Expert")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ConsultationBookingResponse>>>> GetExpertBookings()
    {
        var expertId = GetCurrentUserId();
        var result = await _bookingService.GetExpertBookingsAsync(expertId);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }
}
