using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/v1/consultation-bookings")]
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
    public async Task<ActionResult<ConsultationBookingResponse>> CreateBooking([FromBody] CreateConsultationBookingRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _bookingService.CreateScheduledBookingAsync(userId, request);
        return Ok(result);
    }

    [HttpGet("my-bookings")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<IEnumerable<ConsultationBookingResponse>>> GetMyBookings()
    {
        var userId = GetCurrentUserId();
        var result = await _bookingService.GetMyBookingsAsync(userId);
        return Ok(result);
    }
}
