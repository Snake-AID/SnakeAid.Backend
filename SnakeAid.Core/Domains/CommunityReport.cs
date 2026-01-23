using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class CommunityReport : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Point LocationCoordinates { get; set; }
        public string AdditionalDetails { get; set; }
    }
}