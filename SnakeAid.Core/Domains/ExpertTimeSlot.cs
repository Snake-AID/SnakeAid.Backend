using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Domains
{
    public class ExpertTimeSlot
    {
        public Guid Id { get; set; }
        public Guid ExpertId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal Price { get; set; }
        public TimeSlotStatus Status { get; set; }

        [Timestamp]
        public uint Version { get; set; }
    }

    public enum TimeSlotStatus
    {
        Available = 0,
        Reserved = 1,
        Booked = 2,
        Cancelled = 3
    }
}