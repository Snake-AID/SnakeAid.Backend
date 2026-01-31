using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class RescueRequestSession : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Incident))]
        public Guid IncidentId { get; set; }

        [Required]
        public int SessionNumber { get; set; }        // 1, 2, 3, 4, 5, 6

        [Required]
        public int RadiusKm { get; set; }             // 5, 10, 20 - radius hiện tại đang quét

        [Required]
        public SessionStatus Status { get; set; } = SessionStatus.Active;

        public DateTime? CompletedAt { get; set; }

        // Tracking fields
        [Required]
        public SessionTrigger TriggerType { get; set; } = SessionTrigger.Initial;

        [Required]
        public int RescuersPinged { get; set; } = 0;  // Số lượng rescuers được ping


        // Navigation properties
        public SnakebiteIncident Incident { get; set; }
        public ICollection<RescuerRequest> Requests { get; set; } = new List<RescuerRequest>();
    }

    public enum SessionStatus
    {
        Active = 0,      // Đang chờ response
        Completed = 1,   // Có người accept
        Failed = 2,      // Tất cả expired/rejected
        Cancelled = 3    // User cancel
    }

    public enum SessionTrigger
    {
        Initial = 0,           // Session đầu tiên
        RadiusExpanded = 1,    // Mở rộng radius do session trước fail
        MissionCancelled = 2   // Retry do rescuer cancel mission
    }
}