using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.SnakebiteIncident
{
    public class DispatchIncidentRequest
    {
        [Required]
        public Guid RescuerId { get; set; }

        public bool AllowOffDuty { get; set; } = false;

        public string? OperatorNote { get; set; }
    }
}
