using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class SnakeCatchingRequest : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Address { get; set; }
        public Point LocationCoordinates { get; set; }
        public string AdditionalDetails { get; set; }
        public RequestStatus Status { get; set; } = RequestStatus.Pending;


    }

    public enum RequestStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
}