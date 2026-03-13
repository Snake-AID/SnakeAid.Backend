using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class RescuerRequest : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Incident))]
        public Guid IncidentId { get; set; }  // FK to SnakebiteIncident

        [Required]
        [ForeignKey(nameof(Rescuer))]
        public Guid RescuerId { get; set; }   // FK to RescuerProfile

        [ForeignKey(nameof(Operator))]
        public Guid? OperatorId { get; set; }  // Operator who dispatched this request

        [Required]
        public RescueRequestStatus Status { get; set; } = RescueRequestStatus.Pending;

        [Required]
        public DateTime DispatchedAt { get; set; } = DateTime.UtcNow;  // Khi operator điều phối

        public DateTime? ResponseAt { get; set; }  // Khi rescuer acknowledge/decline

        [MaxLength(500)]
        public string? DeclineReason { get; set; }  // Lý do từ chối (nullable)


        // Navigation properties
        public SnakebiteIncident Incident { get; set; }
        public RescuerProfile Rescuer { get; set; }
        public Account? Operator { get; set; }
    }

    public enum RescueRequestStatus
    {
        Pending = 0,    // Đang chờ rescuer xác nhận
        Accepted = 1,   // Rescuer đã acknowledge, đang chuẩn bị
        Declined = 2,   // Rescuer từ chối (kèm lý do)
        Cancelled = 3   // Đã hủy (do operator redispatch hoặc incident cancelled)
    }
}