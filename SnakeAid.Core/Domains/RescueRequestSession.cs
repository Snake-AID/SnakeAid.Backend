using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class RescueRequestSession : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid IncidentId { get; set; }
        public int SessionNumber { get; set; }        // 1, 2, 3, 4, 5, 6
        public int RadiusKm { get; set; }             // 5, 10, 20
        public int AttemptInRadius { get; set; }      // 1 hoặc 2
        public SessionStatus Status { get; set; }     // Active, Completed, Failed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // các field tracking lại session
        public SessionTrigger TriggerType { get; set; } = SessionTrigger.Initial;
        public int RescuersPinged { get; set; } = 0;  // Số lượng rescuers được ping
        public Guid? CancelledMissionId { get; set; }  // Nếu session này do mission bị cancel

        // Navigation
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