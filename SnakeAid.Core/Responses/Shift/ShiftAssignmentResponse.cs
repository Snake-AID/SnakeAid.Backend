using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.Shift
{
    public class ShiftAssignmentResponse
    {
        public Guid Id { get; set; }
        public Guid RescuerId { get; set; }
        public Guid ShiftId { get; set; }
        public DateOnly Date { get; set; }
        public ShiftAssignmentStatus Status { get; set; }
        public DateTime? CheckInAt { get; set; }
        public DateTime? CheckOutAt { get; set; }
        public string? Notes { get; set; }

        public WorkShiftResponse Shift { get; set; }
    }
}
