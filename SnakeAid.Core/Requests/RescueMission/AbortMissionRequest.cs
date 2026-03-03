using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.RescueMission
{
    public class AbortMissionRequest
    {
        [Required(ErrorMessage = "Cancellation reason is required")]
        [MaxLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters")]
        public string CancellationReason { get; set; } = string.Empty;
    }
}
