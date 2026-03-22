using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.Shift
{
    public class UpdateShiftAssignmentRequest
    {
        [Required]
        public Guid RescuerId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public ShiftAssignmentStatus? Status { get; set; }
    }
}
