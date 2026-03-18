using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.SnakebiteIncident
{
    public class DispatchIncidentRequest
    {
        [Required]
        public Guid RescuerId { get; set; }
    }
}
