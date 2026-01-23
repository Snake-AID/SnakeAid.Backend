namespace SnakeAid.Core.Domains
{
    public class SnakeCatchingMission : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid RescuerId { get; set; }
        public Guid SnakeCatchingRequestId { get; set; }
        public CatchingMissionStatus Status { get; set; } = CatchingMissionStatus.Pending;

        public SnakeCatchingRequest SnakeCatchingRequest { get; set; }
    }

    public enum CatchingMissionStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
}