using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.FirstAid;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.MemberProfile;
using SnakeAid.Core.Responses.RescueMission;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.SnakeSpecies;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class DetailSnakebiteIncidentResponse
    {
        public Guid Id { get; set; }

        public GeoPointResponse LocationCoordinates { get; set; }

        public List<ReportSymptom>? SymptomsReport { get; set; }

        public SnakebiteIncidentStatus Status { get; set; } = SnakebiteIncidentStatus.Pending;

        // Assigned rescuer info
        public DateTime? AssignedAt { get; set; }

        public Guid? AssignedRescuerId { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public int? SeverityLevel { get; set; } = 1;

        public DateTime? IncidentOccurredAt { get; set; }

        /// Loài rắn đã được xác định (nếu có)
        public SnakeSpeciesResponse? IdentifiedSnake { get; set; }

        /// Context về cách xác định loài rắn (nếu có)
        public SnakeIdentificationContext? IdentificationContext { get; set; }

        // Navigation properties
        public BriefMemberProfileResponse User { get; set; }
        public BriefRescuerProfileResponse? AssignedRescuer { get; set; }

        public CreateRescueMissionResponse? ActiveMission { get; set; }

        // How many rescuer had attempted (also includes aborted missions)
        public int TotalRescueAttempts { get; set; }

        /// Number of rescue missions that were aborted by rescuers.
        public int FailedAttemptsCount { get; set; }

        public List<SnakeAIDetectMediaResponse> Media { get; set; } = new List<SnakeAIDetectMediaResponse>();
    }
}
