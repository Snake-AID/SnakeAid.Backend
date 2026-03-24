using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakebiteIncident;

namespace SnakeAid.Core.Responses.SnakeCatchingRequest
{
    public class OperatorSnakeCatchingRequestSummaryResponse
    {
        public Guid Id { get; set; }

        public RequestStatus Status { get; set; }

        public GeoPointResponse LocationCoordinates { get; set; } = default!;

        public DateTime RequestDate { get; set; }
        public string Address { get; set; }

        public Guid? AssignedRescuerId { get; set; }

        public Guid? HandlingOperatorId { get; set; }

        public RequestPriority Priority { get; set; }
    }
}
