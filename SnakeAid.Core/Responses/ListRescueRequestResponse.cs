using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses
{
    public class ListRescueRequestResponse
    {
        public Guid Id { get; set; }

        
        public Guid SessionId { get; set; }  // FK to RescueRequestSession (quan trọng!)

        
        public Guid IncidentId { get; set; }  // FK to SnakebiteIncident

        
        public Guid RescuerId { get; set; }   // FK to RescuerProfile

        public RescueRequestStatus Status { get; set; } = RescueRequestStatus.Pending;

        public DateTime RequestSentAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResponseAt { get; set; }  // Khi rescuer accept/reject

        public DateTime ExpiredAt { get; set; }   // Auto-expire after X minutes
    }
}
