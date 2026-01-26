using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Email;
using SnakeAid.Core.Responses.Email;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

/// <summary>
/// Email Controller - handles email operations including OTP sending
/// </summary>
[ApiController]
[Route("api/email")]
public class EmailController : BaseController<EmailController>
{
    private readonly IEmailService _emailService;

    public EmailController(
        ILogger<EmailController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IEmailService emailService)
        : base(logger, httpContextAccessor, mapper)
    {
        _emailService = emailService;
    }

    /// <summary>
    /// Send OTP verification code to email
    /// </summary>
    [HttpPost("send-otp")]
    [SwaggerOperation(Summary = "Send OTP", Description = "Send OTP verification code to user's email address")]
    [SwaggerResponse(200, "OTP sent successfully", typeof(ApiResponse<SendOtpEmailResponse>))]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(500, "Failed to send OTP")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpEmailRequest request)
    {
        try
        {
            var response = await _emailService.SendOtpEmailAsync(request);
            
            return Ok(ApiResponseBuilder.BuildSuccessResponse(
                response, 
                "OTP has been sent to your email. Please check your inbox."));
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found when sending OTP to {Email}", request.Email);
            return NotFound(ApiResponseBuilder.CreateResponse<SendOtpEmailResponse>(
                null,
                false,
                $"User with email {request.Email} not found.",
                System.Net.HttpStatusCode.NotFound,
                "USER_NOT_FOUND"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP to {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseBuilder.CreateResponse<SendOtpEmailResponse>(
                    null,
                    false,
                    "Failed to send OTP email. Please try again later.",
                    System.Net.HttpStatusCode.InternalServerError,
                    "OTP_SEND_FAILED"));
        }
    }
}
