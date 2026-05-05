using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SystemSetting;
using SnakeAid.Core.Services;
using SnakeAid.Core.Validators;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[Route("api/admin/system-settings")]
[Authorize]
[ApiController]
public class SystemSettingController : BaseController<SystemSettingController>
{
    private readonly ISystemSettingService _systemSettingService;

    public SystemSettingController(
        ILogger<SystemSettingController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ISystemSettingService systemSettingService)
        : base(logger, httpContextAccessor, mapper)
    {
        _systemSettingService = systemSettingService;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Get all system settings")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _systemSettingService.GetAllAsync();
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpGet("{key}")]
    [SwaggerOperation(Summary = "Get system setting by key")]
    public async Task<IActionResult> GetByKey([FromRoute] string key)
    {
        var result = await _systemSettingService.GetByKeyAsync(key);

        if (result == null)
        {
            return NotFound(ApiResponseBuilder.BuildNotFoundResponse($"System setting '{key}' not found."));
        }

        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPut("{key}")]
    [SwaggerOperation(Summary = "Create or update a system setting and refresh cache key")]
    [ValidateModel]
    public async Task<IActionResult> Upsert([FromRoute] string key, [FromBody] UpsertSystemSettingRequest request)
    {
        var result = await _systemSettingService.UpsertAsync(key, request.Value, request.ValueType, request.Description);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "System setting saved and cache refreshed."));
    }

    [HttpPost("reload/{key}")]
    [SwaggerOperation(Summary = "Reload one setting from DB to cache")]
    public async Task<IActionResult> ReloadKey([FromRoute] string key)
    {
        await _systemSettingService.RefreshSettingAsync(key);
        var refreshed = await _systemSettingService.GetByKeyAsync(key);

        return Ok(ApiResponseBuilder.BuildSuccessResponse(new
        {
            key,
            exists = refreshed != null
        }, "System setting key reloaded."));
    }

    [HttpPost("reload-all")]
    [SwaggerOperation(Summary = "Reload all settings from DB to cache")]
    public async Task<IActionResult> ReloadAll()
    {
        await _systemSettingService.RefreshAllSettingsAsync();
        var all = await _systemSettingService.GetAllAsync();

        return Ok(ApiResponseBuilder.BuildSuccessResponse(new
        {
            count = all.Count
        }, "All system settings reloaded."));
    }
}
