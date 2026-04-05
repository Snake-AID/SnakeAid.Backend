namespace SnakeAid.Core.Requests.Statistic;

public class CaseAnalyticsQueryRequest
{
    public string Period { get; set; } = "day";
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}
