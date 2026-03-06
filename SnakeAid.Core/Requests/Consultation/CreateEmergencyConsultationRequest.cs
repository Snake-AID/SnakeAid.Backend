using System;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Consultation;

public class CreateEmergencyConsultationRequest
{
    [Required]
    public Guid ExpertId { get; set; }
}
