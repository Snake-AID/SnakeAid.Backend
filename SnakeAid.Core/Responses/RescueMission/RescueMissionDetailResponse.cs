using System;
using System.Collections.Generic;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.FirstAid;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.MemberProfile;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Core.Responses.SnakeSpecies;

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

        // Distance from rescue center to incident (pricing basis)
        public decimal? DistanceFromCenterKm { get; set; }
        public decimal? CostFromCenter { get; set; }
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
        public string? Address { get; set; }
        public SnakebiteIncidentStatus Status { get; set; }
        public List<ReportSymptom>? SymptomsReport { get; set; }
        public int? SeverityLevel { get; set; }
        public DateTime? IncidentOccurredAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public SnakeSpeciesResponse? IdentifiedSnake { get; set; }

        /// Context về cách xác định loài rắn (nếu có)
        public SnakeIdentificationContext? IdentificationContext { get; set; }

        public List<SnakeAIDetectMediaResponse> Media { get; set; } = new List<SnakeAIDetectMediaResponse>();
    }
}
