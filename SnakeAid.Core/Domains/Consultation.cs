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
        public ConsultationStatus Status { get; set; }
        public ConsultationType Type { get; set; }

    }

    public enum ConsultationStatus
    {
        Scheduled = 0,
        Ongoing = 1,
        Completed = 2,
        Cancelled = 3,
        UserAbsent = 4,
        ExpertAbsent = 5,
        AllAbsent = 6
    }

    public enum ConsultationType
    {
        Emergency = 0,
        Scheduled = 1
    }
}