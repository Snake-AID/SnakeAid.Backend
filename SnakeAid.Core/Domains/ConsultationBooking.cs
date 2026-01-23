using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ConsultationBooking : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ExpertId { get; set; }
        public decimal Price { get; set; }
        public DateTime BookedAt { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }

        public Guid? ConsultationId { get; set; }
        public Guid TimeSlotId { get; set; }

        public ExpertTimeSlot TimeSlot { get; set; }
        public Consultation? Consultation { get; set; }

        [Timestamp]
        public uint Version { get; set; }

    }

    public enum BookingStatus
    {
        PendingPayment = 0,
        Confirmed = 1,
        Cancelled = 2,
        Refunded = 3,
        Expired = 4,
        Completed = 5

    }
}