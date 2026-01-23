using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ConsultationBooking : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey(nameof(Expert))]
        public Guid ExpertId { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public DateTime BookedAt { get; set; }

        [Required]
        public DateTime? PaymentDeadline { get; set; }

        [Required]
        public BookingStatus Status { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? CancellationReason { get; set; }

        [ForeignKey(nameof(Consultation))]
        public Guid? ConsultationId { get; set; }

        [Required]
        [ForeignKey(nameof(TimeSlot))]
        public Guid TimeSlotId { get; set; }

        [Timestamp]
        public uint Version { get; set; }


        public Account User { get; set; }
        public Account Expert { get; set; }
        public ExpertTimeSlot TimeSlot { get; set; }
        public Consultation? Consultation { get; set; }

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