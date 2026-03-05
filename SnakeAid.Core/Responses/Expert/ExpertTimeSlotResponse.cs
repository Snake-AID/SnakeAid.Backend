using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.Expert
{
    public class ExpertTimeSlotResponse
    {
        public Guid Id { get; set; }
        public Guid ExpertId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSlotStatus Status { get; set; }
    }
}
