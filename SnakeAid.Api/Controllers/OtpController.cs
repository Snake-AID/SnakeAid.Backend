using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Otp;
using SnakeAid.Core.Responses.Otp;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/otp")]
public class OtpController : BaseController<OtpController>
{
    private readonly IOtpService _otpService;

    public OtpController(
        ILogger<OtpController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IOtpService otpService)
        : base(logger, httpContextAccessor, mapper)
    {
        _otpService = otpService;
    }

    /// <summary>
    /// Check OTP without consuming it
    /// </summary>
    [HttpPost("check")]
    [SwaggerOperation(Summary = "Check OTP", Description = "Validate OTP code without consuming it (for preview purposes)")]
    [SwaggerResponse(200, "OTP is valid", typeof(ApiResponse<ValidateOtpResponse>))]
    [SwaggerResponse(400, "Invalid OTP or validation error")]
    public async Task<IActionResult> CheckOtp([FromBody] ValidateOtpRequest request)
    {
        var response = await _otpService.CheckOtp(request.Email, request.Otp);

        if (response.Success)
        {
            return Ok(ApiResponseBuilder.BuildSuccessResponse(response, "OTP is valid (check-only)."));
        }

        return BadRequest(ApiResponseBuilder.CreateResponse(
            response,
            false,
            response.Message,
            System.Net.HttpStatusCode.BadRequest,
            "OTP_CHECK_FAILED"));
    }

    /// <summary>
    /// Validate and consume OTP
    /// </summary>
    [HttpPost("validate")]
    [SwaggerOperation(Summary = "Validate OTP", Description = "Validate and consume OTP code (OTP will be deleted after successful validation)")]
    [SwaggerResponse(200, "OTP validated successfully", typeof(ApiResponse<ValidateOtpResponse>))]
    [SwaggerResponse(400, "Invalid OTP or validation error")]
    public async Task<IActionResult> ValidateOtp([FromBody] ValidateOtpRequest request)
    {
        var response = await _otpService.ValidateOtp(request.Email, request.Otp);

        if (response.Success)
        {
            return Ok(ApiResponseBuilder.BuildSuccessResponse(response, "OTP validated successfully."));
        }

        return BadRequest(ApiResponseBuilder.CreateResponse(
            response,
            false,
            response.Message,
            System.Net.HttpStatusCode.BadRequest,
            "OTP_VALIDATION_FAILED"));
    }
}
