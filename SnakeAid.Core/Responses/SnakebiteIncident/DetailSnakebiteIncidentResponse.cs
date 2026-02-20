using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.MemberProfile;
using SnakeAid.Core.Responses.RescueMission;
using SnakeAid.Core.Responses.RescueRequestSession;
using SnakeAid.Core.Responses.RescuerProfile;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class DetailSnakebiteIncidentResponse
    {
        public Guid Id { get; set; }

        public GeoPointResponse LocationCoordinates { get; set; }

        public string? SymptomsReport { get; set; }

        public SnakebiteIncidentStatus Status { get; set; } = SnakebiteIncidentStatus.Pending;

        // Session ping info
        public int CurrentSessionNumber { get; set; }

        public int CurrentRadiusKm { get; set; }

        public DateTime? LastSessionAt { get; set; }

        // Assigned rescuer info
        public DateTime? AssignedAt { get; set; }

        public Guid? AssignedRescuerId { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public int? SeverityLevel { get; set; } = 1;

        public DateTime? IncidentOccurredAt { get; set; }

        // Navigation properties
        public BriefMemberProfileResponse User { get; set; }
        public BriefRescuerProfileResponse? AssignedRescuer { get; set; }

        public CreateRescueMissionResponse? RescueMission { get; set; }
        public List<SnakeAIDetectMediaResponse> Media { get; set; } = new List<SnakeAIDetectMediaResponse>();
    }
}
