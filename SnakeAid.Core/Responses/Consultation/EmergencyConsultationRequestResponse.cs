using SnakeAid.Core.Domains;
using System;

namespace SnakeAid.Core.Responses.Consultation;

public class EmergencyConsultationRequestResponse
{
    public Guid RequestId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid ExpertId { get; set; }
    public ConsultationPingStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public Guid? ConsultationId { get; set; }
    public string? RoomId { get; set; }
}
