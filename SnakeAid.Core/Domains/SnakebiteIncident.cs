using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class SnakebiteIncident : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }  // FK to MemberProfile
        public string Location { get; set; }
        public Point LocationCoordinates { get; set; }
        public string SymptomsReport { get; set; }
        public SnakebiteIncidentStatus Status { get; set; } = SnakebiteIncidentStatus.Pending;

        // session ping info
        public int CurrentSessionNumber { get; set; } = 0;   // Track session hiện tại
        public int CurrentRadiusKm { get; set; } = 5;        // Radius hiện tại
        public DateTime? LastSessionAt { get; set; }         // Tránh spam sessions

        // assigned rescuer info
        public DateTime? AssignedAt { get; set; }
        public Guid? AssignedRescuerId { get; set; }  // FK to assigned rescuer

        // Navigation properties
        public MemberProfile User { get; set; }
        public RescuerProfile? AssignedRescuer { get; set; }
        public ICollection<RescueRequestSession> Sessions { get; set; } = new List<RescueRequestSession>();
        public ICollection<RescuerRequest> AllRequests { get; set; } = new List<RescuerRequest>(); // Denormalized for easy query
        public RescueMission? RescueMission { get; set; }
    }

    public enum SnakebiteIncidentStatus
    {
        Pending = 0,
        Assigned = 1,
        Finished = 2,
        Cancelled = 3,
        NoRescuerFound = 4,
        Paid = 5,
        Disputed = 6,
        Completed = 7
    }
}