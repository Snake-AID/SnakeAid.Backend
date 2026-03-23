using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Shift
{
    public class AssignWorkShiftBulkRequest
    {
        [Required]
        [MinLength(1)]
        public List<Guid> RescuerIds { get; set; } = new();

        [Required]
        public DateOnly Date { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
