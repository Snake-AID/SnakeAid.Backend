namespace SnakeAid.Core.Responses.Statistic;

public class ExpertTodayStatisticsResponse
{
    public string Period { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public int ConsultationRequests { get; set; }
    public int CompletedConsultations { get; set; }
    public decimal TotalIncome { get; set; }
    public string Currency { get; set; } = "VND";
}
