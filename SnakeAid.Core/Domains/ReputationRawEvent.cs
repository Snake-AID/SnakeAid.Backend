using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ReputationRawEvent : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; }  // "mission_completed", "consultation_rated"

        [Required]
        public Guid ReferenceId { get; set; }  // ID của entity liên quan

        [Required]
        public string ReferenceType { get; set; }  // "Mission", "Consultation", "CatchingRequest"

        [Required]
        public int PointsChange { get; set; }  // +/- điểm sẽ thay đổi

        [Required]
        public bool IsProcessed { get; set; } = false;

        public DateTime? ProcessedAt { get; set; }

        public string? ProcessingError { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        public Account User { get; set; }
    }
}