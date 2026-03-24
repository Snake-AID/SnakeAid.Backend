namespace SnakeAid.Core.Responses.Shift
{
    public class WorkShiftResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int RequiredRescuers { get; set; }
        public bool IsActive { get; set; }
    }
}
