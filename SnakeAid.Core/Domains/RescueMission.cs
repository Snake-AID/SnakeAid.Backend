using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class RescueMission : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Incident))]
        public Guid IncidentId { get; set; }  // FK to SnakebiteIncident (1-1)

        [Required]
        [ForeignKey(nameof(Rescuer))]
        public Guid RescuerId { get; set; }   // FK to RescuerProfile

        [Required]
        public RescueMissionStatus Status { get; set; } = RescueMissionStatus.Preparing;

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? ArrivedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal? EstimatedCost { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [Range(0, double.MaxValue)]
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