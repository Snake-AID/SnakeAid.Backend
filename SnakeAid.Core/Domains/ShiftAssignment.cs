using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnakeAid.Core.Domains
{
    public class ShiftAssignment : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Rescuer))]
        public Guid RescuerId { get; set; }

        [Required]
        [ForeignKey(nameof(Shift))]
        public Guid ShiftId { get; set; }

        [Required]
        public DateTime ShiftStartLocal { get; set; }

        [Required]
        public DateTime ShiftEndLocal { get; set; }

        [Required]
        public ShiftAssignmentStatus Status { get; set; } = ShiftAssignmentStatus.Scheduled;

        public DateTime? CheckInAtUtc { get; set; }

        public DateTime? CheckOutAtUtc { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public RescuerProfile Rescuer { get; set; }
        public WorkShift Shift { get; set; }
    }

    public enum ShiftAssignmentStatus
    {
        Scheduled = 0,
        Active = 1,
        Completed = 2,
        Cancelled = 3,
        NoShow = 4
    }
}
