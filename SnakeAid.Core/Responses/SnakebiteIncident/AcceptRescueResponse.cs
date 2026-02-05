using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class AcceptRescueResponse
    {
        public Guid RequestId { get; set; }
        public Guid IncidentId { get; set; }
        public Guid RescuerId { get; set; }
        public Guid MissionId { get; set; }
        public DateTime AcceptedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
