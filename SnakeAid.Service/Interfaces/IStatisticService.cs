using SnakeAid.Core.Requests.Statistic;
using SnakeAid.Core.Responses.Statistic;

namespace SnakeAid.Service.Interfaces;

public interface IStatisticService
{
    Task<RevenueAnalyticsResponse> GetRevenueAsync(RevenueAnalyticsQueryRequest request, CancellationToken cancellationToken = default);
    Task<CommissionAnalyticsResponse> GetCommissionAsync(CommissionAnalyticsQueryRequest request, CancellationToken cancellationToken = default);
    Task<ProfitAnalyticsResponse> GetProfitAsync(ProfitAnalyticsQueryRequest request, CancellationToken cancellationToken = default);
    Task<UserAnalyticsResponse> GetUsersAsync(UserAnalyticsQueryRequest request, CancellationToken cancellationToken = default);
    Task<CaseAnalyticsResponse> GetCasesAsync(CaseAnalyticsQueryRequest request, CancellationToken cancellationToken = default);
    Task<RescuerTodayStatisticsResponse> GetRescuerStatisticsAsync(Guid rescuerId, string period, CancellationToken cancellationToken = default);
    Task<ExpertTodayStatisticsResponse> GetExpertStatisticsAsync(Guid expertId, string period, CancellationToken cancellationToken = default);
}
