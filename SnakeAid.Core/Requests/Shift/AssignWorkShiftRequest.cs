using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Shift
{
    public class AssignWorkShiftRequest
    {
        [Required]
        public Guid RescuerId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
