using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ExpertTimeSlot
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Expert))]
        public Guid ExpertId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        public TimeSlotStatus Status { get; set; }

        [Timestamp]
        public uint Version { get; set; }


        public Account Expert { get; set; }
        public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
        public ICollection<ConsultationBooking> RescueMissions { get; set; } = new List<ConsultationBooking>();
    }

    public enum TimeSlotStatus
    {
        Available = 0,
        Reserved = 1,
        Booked = 2,
        Cancelled = 3
    }
}