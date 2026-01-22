using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class RescueMission : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid IncidentId { get; set; }  // FK to SnakebiteIncident (1-1)
        public Guid RescuerId { get; set; }   // FK to RescuerProfile
        public RescueMissionStatus Status { get; set; } = RescueMissionStatus.Preparing;
        public DateTime? StartedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? ActualCost { get; set; }

        // Navigation properties
        public SnakebiteIncident Incident { get; set; }
        public RescuerProfile Rescuer { get; set; }
    }

    public enum RescueMissionStatus
    {
        Preparing = 0,
        EnRoute = 1,
        RescuerArrived = 2,
        MissionCompleted = 3,
        MissionUncompleted = 4,
        MissionAborted = 5,
        Cancelled = 6,
    }
}