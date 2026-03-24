using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class OperatorIncidentSummaryResponse
    {
        public Guid Id { get; set; }

        public SnakebiteIncidentStatus Status { get; set; }

        public GeoPointResponse LocationCoordinates { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
        public string Address { get; set; }

        public Guid? AssignedRescuerId { get; set; }

        public RescueMissionStatus? ActiveMissionStatus { get; set; }

        public bool NeedsRedispatch { get; set; }

        public Guid? HandlingOperatorId { get; set; }
    }
}
