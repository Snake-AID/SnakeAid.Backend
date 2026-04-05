namespace SnakeAid.Core.Responses.Statistic;

public class CommissionAnalyticsResponse
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
    public decimal TotalCommission { get; set; }
    public string RateNote { get; set; } = "Hoa hồng = ConsultationPayment - ExpertPayout - ConsultationRefund";
    public List<CommissionTimelinePoint> Timeline { get; set; } = new();
}

public class CommissionTimelinePoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal ExpertPayout { get; set; }
    public decimal Refund { get; set; }
    public decimal Commission { get; set; }
}
