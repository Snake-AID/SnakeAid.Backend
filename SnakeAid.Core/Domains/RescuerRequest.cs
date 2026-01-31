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
        [ForeignKey(nameof(Session))]
        public Guid SessionId { get; set; }  // FK to RescueRequestSession (quan trọng!)

        [Required]
        [ForeignKey(nameof(Incident))]
        public Guid IncidentId { get; set; }  // FK to SnakebiteIncident

        [Required]
        [ForeignKey(nameof(Rescuer))]
        public Guid RescuerId { get; set; }   // FK to RescuerProfile

        [Required]
        public RescueRequestStatus Status { get; set; } = RescueRequestStatus.Pending;

        [Required]
        public DateTime RequestSentAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResponseAt { get; set; }  // Khi rescuer accept/reject

        [Required]
        public DateTime ExpiredAt { get; set; }   // Auto-expire after X minutes


        // Navigation properties
        public RescueRequestSession Session { get; set; }
        public SnakebiteIncident Incident { get; set; }
        public RescuerProfile Rescuer { get; set; }
    }

    public enum RescueRequestStatus
    {
        Pending = 0,    // Đang chờ rescuer phản hồi
        Accepted = 1,   // Rescuer đồng ý giúp
        Rejected = 2,   // Rescuer từ chối
        Taken = 3,      // Bị lụm bởi rescuer khác
        Cancelled = 4,  // Bị user cancel
        Expired = 5     // Hết hạn chờ phản hồi
    }
}