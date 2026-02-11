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


        public Guid UserId { get; set; }  // FK to MemberProfile

        [Column(TypeName = "geometry(Point, 4326)")]
        public Point LocationCoordinates { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SymptomsReport { get; set; }

        public SnakebiteIncidentStatus Status { get; set; } = SnakebiteIncidentStatus.Pending;

        // Session ping info
        public int CurrentSessionNumber { get; set; } = 0;   // Track session hiện tại

        [Range(1, 50)]
        public int CurrentRadiusKm { get; set; } = 5;        // Radius hiện tại

        public DateTime? LastSessionAt { get; set; }         // Tránh spam sessions

        // Assigned rescuer info
        public DateTime? AssignedAt { get; set; }

        [ForeignKey(nameof(AssignedRescuer))]
        public Guid? AssignedRescuerId { get; set; }  // FK to assigned rescuer

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public int? SeverityLevel { get; set; } = 1;  // 1-5 emergency level

        public DateTime? IncidentOccurredAt { get; set; }  // Khi nào bị cắn

        // Navigation properties
        public BriefMemberProfileRespone User { get; set; }
        public BriefRescuerProfileResponse? AssignedRescuer { get; set; }
        public List<CreateRescueRequestSessionResponse> Sessions { get; set; } = new List<CreateRescueRequestSessionResponse>();
        public List<ListRescueRequestResponse> AllRequests { get; set; } = new List<ListRescueRequestResponse>(); // Denormalized for easy query
        public CreateRescueMissionResponse? RescueMission { get; set; }
        public List<ReportMediaResponse> Media { get; set; } = new List<ReportMediaResponse>();
    }
}
