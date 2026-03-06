using System;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Consultation;

public class CreateConsultationBookingRequest
{
    [Required]
    public Guid TimeSlotId { get; set; }

    [MaxLength(2000)]
    public string? ProblemDescription { get; set; }
}
