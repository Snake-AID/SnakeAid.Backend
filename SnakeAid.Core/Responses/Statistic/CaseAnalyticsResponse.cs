namespace SnakeAid.Core.Responses.Statistic;

public class CaseAnalyticsResponse
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int TotalCases { get; set; }
    public int SnakebiteCases { get; set; }
    public int SnakeCatchingCases { get; set; }
    public List<CaseTimelinePoint> Timeline { get; set; } = new();
}

public class CaseTimelinePoint
{
    public string Label { get; set; } = string.Empty;
    public int TotalCases { get; set; }
    public int SnakebiteCases { get; set; }
    public int SnakeCatchingCases { get; set; }
}
