using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Notification;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[Route("api/notifications/device-token")]
[ApiController]
[Authorize]
public class NotificationDeviceController : BaseController<NotificationDeviceController>
{
    private readonly UserManager<Account> _userManager;

    public NotificationDeviceController(
        ILogger<NotificationDeviceController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        UserManager<Account> userManager)
        : base(logger, httpContextAccessor, mapper)
    {
        _userManager = userManager;
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Update my device token", Description = "Store FCM token for current authenticated user")]
    public async Task<IActionResult> UpdateMyDeviceToken([FromBody] UpdateDeviceTokenRequest request)
    {
        var userId = GetCurrentUserId();
        var account = await _userManager.FindByIdAsync(userId.ToString());

        if (account == null)
        {
            return NotFound(ApiResponseBuilder.BuildErrorResponse("User not found"));
        }

        account.FcmToken = request.DeviceToken.Trim();
        account.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(account);
        if (!result.Succeeded)
        {
            return BadRequest(ApiResponseBuilder.BuildErrorResponse("Failed to update device token"));
        }

        return Ok(ApiResponseBuilder.BuildSuccessResponse("Device token updated successfully"));
    }

    [HttpDelete]
    [SwaggerOperation(Summary = "Clear my device token", Description = "Remove FCM token for current authenticated user")]
    public async Task<IActionResult> ClearMyDeviceToken()
    {
        var userId = GetCurrentUserId();
        var account = await _userManager.FindByIdAsync(userId.ToString());

        if (account == null)
        {
            return NotFound(ApiResponseBuilder.BuildErrorResponse("User not found"));
        }

        account.FcmToken = null;
        account.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(account);
        if (!result.Succeeded)
        {
            return BadRequest(ApiResponseBuilder.BuildErrorResponse("Failed to clear device token"));
        }

        return Ok(ApiResponseBuilder.BuildSuccessResponse("Device token cleared successfully"));
    }
}
