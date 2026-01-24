using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class SnakeCatchingMission : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Rescuer))]
        public Guid RescuerId { get; set; }

        [Required]
        [ForeignKey(nameof(SnakeCatchingRequest))]
        public Guid SnakeCatchingRequestId { get; set; }

        [Required]
        public CatchingMissionStatus Status { get; set; } = CatchingMissionStatus.Preparing;

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Price { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? ArrivedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal? EstimatedCost { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal? ActualCost { get; set; }

        // Navigation properties
        public RescuerProfile Rescuer { get; set; }
        public SnakeCatchingRequest SnakeCatchingRequest { get; set; }
    }

    public enum CatchingMissionStatus
    {
        Preparing = 0,
        EnRoute = 1,
        Arrived = 2,
        MissionCompleted = 3,
        MissionUncompleted = 4,
        MissionAborted = 5,
        Cancelled = 6
    }
}