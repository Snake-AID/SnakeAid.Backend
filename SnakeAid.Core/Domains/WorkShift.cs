using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Domains
{
    public class WorkShift : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        [Range(0, int.MaxValue)]
        public int RequiredRescuers { get; set; } = 0;

        public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
    }
}
