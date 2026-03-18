using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class SnakeCatchingRequest : BaseEntity, IHasReportMedia
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

        [Timestamp]
        public uint Version { get; set; }

        [ForeignKey(nameof(HandlingOperator))]
        public Guid? HandlingOperatorId { get; set; }

        [MaxLength(1000)]
        public string? OperatorNotes { get; set; }

        [Required]
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;

        [Required]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public DateTime? PreferredTime { get; set; }

        public DateTime? DispatchedAt { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        public DateTime? AssignedAt { get; set; }
        public DateTime? PrePaidAt { get; set; }
        public bool IsPrePaid { get; set; } = false;

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
        public Account? HandlingOperator { get; set; }
        public RescuerProfile? AssignedRescuer { get; set; }
        public ICollection<SnakeCatchingMission> Missions { get; set; } = new List<SnakeCatchingMission>();
        public ICollection<ReportMedia> Media { get; set; } = new List<ReportMedia>();
        public ICollection<CatchingRequestDetail> Details { get; set; } = new List<CatchingRequestDetail>();
    }

    public enum RequestStatus
    {
        Pending = 0,
        OperatorContacting = 1,
        Confirmed = 2,
        Assigned = 3,
        Finished = 4,
        Paid = 5,
        Disputed = 6,
        Completed = 7,
        Cancelled = 8,
        Expired = 9
    }

    public enum RequestPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }
}