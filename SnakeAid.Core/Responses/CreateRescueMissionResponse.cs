using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses
{
    public class CreateRescueMissionResponse
    {
        public Guid Id { get; set; }

        
        public Guid IncidentId { get; set; }  // FK to SnakebiteIncident (1-1)

        
        public Guid RescuerId { get; set; }   // FK to RescuerProfile

        public RescueMissionStatus Status { get; set; } = RescueMissionStatus.Preparing;

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
    }
}
