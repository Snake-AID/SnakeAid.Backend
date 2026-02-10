using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.RescueMission
{
    public class RescueMissionStatusResponse
    {
        public Guid Id { get; set; }
        public Guid IncidentId { get; set; }
        public Guid RescuerId { get; set; }
        public RescueMissionStatus Status { get; set; }
        public RescueMissionStatus PreviousStatus { get; set; }
        public decimal Price { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? ActualCost { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Related incident status
        public SnakebiteIncidentStatus? IncidentStatus { get; set; }
        public int VerificationImageCount { get; set; }
    }
}
