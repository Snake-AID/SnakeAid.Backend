namespace SnakeAid.Core.Requests.SnakebiteIncident
{
    public class ReportNoAnswerRequest
    {
        public bool ContinueCalling { get; set; } = true;
        public string? Note { get; set; }
    }
}
