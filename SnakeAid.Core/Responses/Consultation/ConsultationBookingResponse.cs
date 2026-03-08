using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.Consultation;

public class ConsultationBookingResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public Guid ExpertId { get; set; }
    public string? ExpertName { get; set; }
    public decimal Price { get; set; }
    public DateTime BookedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public BookingStatus Status { get; set; }
    public string? ProblemDescription { get; set; }
    public Guid TimeSlotId { get; set; }
    public DateTime SlotStartTime { get; set; }
    public DateTime SlotEndTime { get; set; }
    public Guid? ConsultationId { get; set; }
    public string? RoomId { get; set; }
}
