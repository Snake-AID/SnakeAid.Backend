using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.SnakeCatchingMission
{
    public class AbortSnakeCatchingMissionRequest
    {
        [Required(ErrorMessage = "Reason is required")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;
    }
}
