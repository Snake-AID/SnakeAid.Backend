using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Shift
{
    public class CreateWorkShiftRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Range(0, int.MaxValue)]
        public int RequiredRescuers { get; set; } = 0;
    }
}
