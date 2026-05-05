using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Statistic;
using SnakeAid.Core.Responses.Statistic;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[Authorize(Roles = "Admin, Rescuer, Expert")]
[ApiController]
[Route("api/admin/analytics")]
public class StatisticController : BaseController<StatisticController>
{
    private readonly IStatisticService _statisticService;

    public StatisticController(
        IStatisticService statisticService,
        ILogger<StatisticController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper)
        : base(logger, httpContextAccessor, mapper)
    {
        _statisticService = statisticService;
    }

    [HttpGet("revenue")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<RevenueAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenue([FromQuery] RevenueAnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var data = await _statisticService.GetRevenueAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(data));
    }

    [HttpGet("commission")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CommissionAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCommission([FromQuery] CommissionAnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var data = await _statisticService.GetCommissionAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(data));
    }

    [HttpGet("profit")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProfitAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfit([FromQuery] ProfitAnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var data = await _statisticService.GetProfitAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(data));
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] UserAnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var data = await _statisticService.GetUsersAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(data));
    }

    [HttpGet("cases")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CaseAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCases([FromQuery] CaseAnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var data = await _statisticService.GetCasesAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(data));
    }

    [HttpGet("/api/analytics/rescuer/statistics")]
    [Authorize(Roles = "Rescuer")]
    [ProducesResponseType(typeof(ApiResponse<RescuerTodayStatisticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRescuerStatistics([FromQuery] RoleStatisticsQueryRequest request, CancellationToken cancellationToken)
    {
        var rescuerId = GetCurrentUserId();
        var data = await _statisticService.GetRescuerStatisticsAsync(rescuerId, request.Period, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(data));
    }

    [HttpGet("/api/analytics/expert/statistics")]
    [Authorize(Roles = "Expert")]
    [ProducesResponseType(typeof(ApiResponse<ExpertTodayStatisticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpertStatistics([FromQuery] RoleStatisticsQueryRequest request, CancellationToken cancellationToken)
    {
        var expertId = GetCurrentUserId();
        var data = await _statisticService.GetExpertStatisticsAsync(expertId, request.Period, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(data));
    }
}
