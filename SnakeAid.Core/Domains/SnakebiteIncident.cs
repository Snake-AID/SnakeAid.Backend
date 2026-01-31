using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class SnakebiteIncident : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }  // FK to MemberProfile

        [Required]
        [Column(TypeName = "geometry(Point, 4326)")]
        public Point LocationCoordinates { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SymptomsReport { get; set; }

        [Required]
        public SnakebiteIncidentStatus Status { get; set; } = SnakebiteIncidentStatus.Pending;

        // Session ping info
        [Required]
        public int CurrentSessionNumber { get; set; } = 0;   // Track session hiện tại

        [Required]
        [Range(1, 50)]
        public int CurrentRadiusKm { get; set; } = 5;        // Radius hiện tại

        public DateTime? LastSessionAt { get; set; }         // Tránh spam sessions

        // Assigned rescuer info
        public DateTime? AssignedAt { get; set; }

        [ForeignKey(nameof(AssignedRescuer))]
        public Guid? AssignedRescuerId { get; set; }  // FK to assigned rescuer

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public int? SeverityLevel { get; set; } = 1;  // 1-5 emergency level

        public DateTime? IncidentOccurredAt { get; set; }  // Khi nào bị cắn

        // Navigation properties
        public MemberProfile User { get; set; }
        public RescuerProfile? AssignedRescuer { get; set; }
        public ICollection<RescueRequestSession> Sessions { get; set; } = new List<RescueRequestSession>();
        public ICollection<RescuerRequest> AllRequests { get; set; } = new List<RescuerRequest>(); // Denormalized for easy query
        public RescueMission? RescueMission { get; set; }
        public ICollection<ReportMedia> Media { get; set; } = new List<ReportMedia>();
    }

    public enum SnakebiteIncidentStatus
    {
        Pending = 0,
        Assigned = 1,
        Finished = 2,
        Cancelled = 3,
        NoRescuerFound = 4,
        Paid = 5,
        Disputed = 6,
        Completed = 7
    }
}