namespace SnakeAid.Core.Responses.Statistic;

public class ProfitAnalyticsResponse
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
    public decimal TotalProfit { get; set; }
    public Dictionary<string, decimal> ByFlow { get; set; } = new();
    public List<Dictionary<string, object>> Timeline { get; set; } = new();
}
