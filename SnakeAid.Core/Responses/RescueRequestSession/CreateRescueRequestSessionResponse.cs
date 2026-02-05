using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.RescueRequestSession
{
    public class CreateRescueRequestSessionResponse
    {
        public Guid Id { get; set; }

        public Guid IncidentId { get; set; }

        public int SessionNumber { get; set; }        // 1, 2, 3, 4, 5, 6

        public int RadiusKm { get; set; }             // 5, 10, 20 - radius hiện tại đang quét

        public SessionStatus Status { get; set; } = SessionStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Tracking fields
        public SessionTrigger TriggerType { get; set; } = SessionTrigger.Initial;

        public int RescuersPinged { get; set; } = 0;  // Số lượng rescuers được ping
    }
}
