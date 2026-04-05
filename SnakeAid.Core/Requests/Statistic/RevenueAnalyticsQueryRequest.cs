namespace SnakeAid.Core.Requests.Statistic;

public class RevenueAnalyticsQueryRequest
{
    public string Period { get; set; } = "day";
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string? Flow { get; set; }
}
