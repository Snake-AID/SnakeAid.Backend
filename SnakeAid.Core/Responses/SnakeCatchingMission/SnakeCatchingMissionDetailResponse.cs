using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.SnakeCatchingMission
{
    public class SnakeCatchingMissionDetailResponse
    {
        public Guid Id { get; set; }
        public Guid RescuerId { get; set; }
        public Guid SnakeCatchingRequestId { get; set; }
        public CatchingMissionStatus Status { get; set; }
        public decimal Price { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? ActualCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
