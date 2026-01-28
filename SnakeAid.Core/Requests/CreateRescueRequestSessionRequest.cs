using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests
{
    public class CreateRescueRequestSessionRequest
    {
        [Required]
        public Guid IncidentId { get; set; }

        [Required]
        public int SessionNumber { get; set; }        // 1, 2, 3, 4, 5, 6

        [Required]
        public int RadiusKm { get; set; }             // 5, 10, 20 - radius hiện tại đang quét

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Tracking fields
        [Required]
        public SessionTrigger TriggerType { get; set; } = SessionTrigger.Initial;

        [Required]
        public int RescuersPinged { get; set; } = 0;  // Số lượng rescuers được ping
    }
}
