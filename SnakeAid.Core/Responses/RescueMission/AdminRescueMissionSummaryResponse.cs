using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.RescueMission
{
    public class AdminRescueMissionSummaryResponse
    {
        public Guid Id { get; set; }

        public Guid IncidentId { get; set; }

        public Guid RescuerId { get; set; }

        public RescueMissionStatus Status { get; set; }

        public decimal Price { get; set; }

        public decimal? ActualCost { get; set; }

        public decimal? CostFromCenter { get; set; } = 0;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? ArrivedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public SnakebiteIncidentStatus IncidentStatus { get; set; }

        public string IncidentAddress { get; set; } = string.Empty;

        public string? RescuerName { get; set; }
    }
}
