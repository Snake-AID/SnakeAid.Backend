using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Statistic;
using SnakeAid.Core.Responses.Statistic;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api.Controllers;

[Authorize(Roles = "Admin")]
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
    [ProducesResponseType(typeof(ApiResponse<RevenueAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenue([FromQuery] RevenueAnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var data = await _statisticService.GetRevenueAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(data));
    }

    [HttpGet("commission")]
    [ProducesResponseType(typeof(ApiResponse<CommissionAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCommission([FromQuery] CommissionAnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var data = await _statisticService.GetCommissionAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(data));
    }

    [HttpGet("profit")]
    [ProducesResponseType(typeof(ApiResponse<ProfitAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfit([FromQuery] ProfitAnalyticsQueryRequest request, CancellationToken cancellationToken)
    {
        var data = await _statisticService.GetProfitAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(data));
    }
}
