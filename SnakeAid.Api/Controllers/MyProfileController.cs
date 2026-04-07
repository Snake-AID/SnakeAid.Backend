using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.MyProfile;
using SnakeAid.Core.Responses.MyProfile;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api")]
public class MyProfileController : BaseController<MyProfileController>
{
    private readonly IMyProfileService _myProfileService;

    public MyProfileController(
        ILogger<MyProfileController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IMyProfileService myProfileService)
        : base(logger, httpContextAccessor, mapper)
    {
        _myProfileService = myProfileService;
    }

    [HttpGet("members/me/profile")]
    [Authorize(Roles = "User")]
    [SwaggerOperation(Summary = "Get current member profile")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<MemberMyProfileResponse>))]
    public async Task<IActionResult> GetMemberProfile()
    {
        var result = await _myProfileService.GetMemberProfileAsync(GetCurrentUserId());
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPut("members/me/profile")]
    [Authorize(Roles = "User")]
    [SwaggerOperation(Summary = "Update current member profile")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<MemberMyProfileResponse>))]
    public async Task<IActionResult> UpdateMemberProfile([FromBody] UpdateMemberProfileRequest request)
    {
        var result = await _myProfileService.UpdateMemberProfileAsync(GetCurrentUserId(), request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Member profile updated successfully."));
    }

    [HttpGet("experts/me/profile")]
    [Authorize(Roles = "Expert")]
    [SwaggerOperation(Summary = "Get current expert profile")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<ExpertMyProfileResponse>))]
    public async Task<IActionResult> GetExpertProfile()
    {
        var result = await _myProfileService.GetExpertProfileAsync(GetCurrentUserId());
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPut("experts/me/profile")]
    [Authorize(Roles = "Expert")]
    [SwaggerOperation(Summary = "Update current expert profile")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<ExpertMyProfileResponse>))]
    public async Task<IActionResult> UpdateExpertProfile([FromBody] UpdateExpertProfileRequest request)
    {
        var result = await _myProfileService.UpdateExpertProfileAsync(GetCurrentUserId(), request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Expert profile updated successfully."));
    }

    [HttpGet("rescuers/me/profile")]
    [Authorize(Roles = "Rescuer")]
    [SwaggerOperation(Summary = "Get current rescuer profile")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<RescuerMyProfileResponse>))]
    public async Task<IActionResult> GetRescuerProfile()
    {
        var result = await _myProfileService.GetRescuerProfileAsync(GetCurrentUserId());
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPut("rescuers/me/profile")]
    [Authorize(Roles = "Rescuer")]
    [SwaggerOperation(Summary = "Update current rescuer profile")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<RescuerMyProfileResponse>))]
    public async Task<IActionResult> UpdateRescuerProfile([FromBody] UpdateRescuerProfileRequest request)
    {
        var result = await _myProfileService.UpdateRescuerProfileAsync(GetCurrentUserId(), request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Rescuer profile updated successfully."));
    }
}
