using System;
using System.Collections.Generic;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.CatchingEnvironment;
using SnakeAid.Core.Responses.Media;

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
        public int? CatchingEnvironmentId { get; set; }
        public CatchingEnvironmentResponse? CatchingEnvironment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CatchingMissionDetailResponse>? MissionDetails { get; set; }
        public List<ReportMediaResponse>? Media { get; set; }
    }
}
