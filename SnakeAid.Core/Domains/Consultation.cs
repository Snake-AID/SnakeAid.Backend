using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class Consultation : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid CallerId { get; set; }
        public Guid CalleeId { get; set; }
        public string RoomId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

    }
}