namespace SnakeAid.Core.Responses.Statistic;

public class UserAnalyticsResponse
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public List<UserTimelinePoint> Timeline { get; set; } = new();
}

public class UserTimelinePoint
{
    public string Label { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
}
