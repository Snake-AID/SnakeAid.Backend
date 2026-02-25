using System;
using System.Collections.Generic;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.MemberProfile;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.SnakebiteIncident;

namespace SnakeAid.Core.Responses.RescueMission
{
    public class DetailRescueMissionResponse
    {
        // Mission basic info
        public Guid Id { get; set; }
        public Guid IncidentId { get; set; }
        public Guid RescuerId { get; set; }

        public RescueMissionStatus Status { get; set; } = RescueMissionStatus.Preparing;

        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Mission details
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }

        // Cost tracking
        public decimal? EstimatedCost { get; set; }
        public decimal? ActualCost { get; set; }

        /// Distance from rescuer to incident (calculated on demand)
        public double? DistanceKm { get; set; }

        public BriefIncidentResponse Incident { get; set; } = null!;

        public BriefRescuerProfileResponse Rescuer { get; set; } = null!;

        public BriefMemberProfileResponse User { get; set; } = null!;
    }

    public class BriefIncidentResponse
    {
        public Guid Id { get; set; }
        public GeoPointResponse LocationCoordinates { get; set; } = null!;
        public SnakebiteIncidentStatus Status { get; set; }
        public string? SymptomsReport { get; set; }
        public int? SeverityLevel { get; set; }
        public DateTime? IncidentOccurredAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public int CurrentSessionNumber { get; set; }
        public int CurrentRadiusKm { get; set; }

        public List<SnakeAIDetectMediaResponse> Media { get; set; } = new List<SnakeAIDetectMediaResponse>();
    }
}
