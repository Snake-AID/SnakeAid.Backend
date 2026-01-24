using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class SnakeCatchingRequest : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Address { get; set; }

        [Required]
        [Column(TypeName = "geometry(Point, 4326)")]
        public Point LocationCoordinates { get; set; }

        [Required]
        [MaxLength(2000)]
        public string AdditionalDetails { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [Required]
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;

        [Required]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public DateTime? PreferredTime { get; set; }

        public DateTime? AssignedAt { get; set; }

        [ForeignKey(nameof(AssignedRescuer))]
        public Guid? AssignedRescuerId { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal? EstimatedPrice { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }


        // Navigation properties
        public MemberProfile User { get; set; }
        public RescuerProfile? AssignedRescuer { get; set; }
        public SnakeCatchingMission? Mission { get; set; }
        public ICollection<ReportMedia> Media { get; set; } = new List<ReportMedia>();
    }

    public enum RequestStatus
    {
        Pending = 0,
        Assigned = 1,
        Finished = 2,
        Paid = 3,
        Disputed = 4,
        Completed = 5,
        Cancelled = 6,
        Expired = 7
    }

    public enum RequestPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }
}