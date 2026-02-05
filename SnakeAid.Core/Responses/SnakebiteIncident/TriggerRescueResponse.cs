using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class TriggerRescueResponse
    {
        public Guid IncidentId { get; set; }
        public Guid SessionId { get; set; }
        public int SessionNumber { get; set; }
        public int RadiusKm { get; set; }
        public int RescuersPinged { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
