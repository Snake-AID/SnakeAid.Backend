using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class RejectRescueResponse
    {
        public Guid RequestId { get; set; }
        public DateTime RejectedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
