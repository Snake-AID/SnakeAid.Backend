namespace SnakeAid.Core.Requests.Statistic;

public class UserAnalyticsQueryRequest
{
    public string Period { get; set; } = "day";
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}
