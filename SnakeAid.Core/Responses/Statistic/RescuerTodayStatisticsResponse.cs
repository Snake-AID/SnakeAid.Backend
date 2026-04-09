namespace SnakeAid.Core.Responses.Statistic;

public class RescuerTodayStatisticsResponse
{
    public string Period { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SnakebiteRequests { get; set; }
    public int SnakeCatchingRequests { get; set; }
    public int TotalCompleted { get; set; }
    public int SnakebiteCompleted { get; set; }
    public int SnakeCatchingCompleted { get; set; }
    public decimal TotalIncome { get; set; }
    public string Currency { get; set; } = "VND";
}
