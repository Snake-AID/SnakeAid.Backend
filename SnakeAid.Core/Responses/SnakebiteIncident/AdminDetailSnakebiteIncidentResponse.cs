using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.FirstAid;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.MemberProfile;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.SnakeSpecies;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class AdminDetailSnakebiteIncidentResponse
    {
        public Guid Id { get; set; }

        public GeoPointResponse LocationCoordinates { get; set; }

        public string Address { get; set; }

        public List<ReportSymptom>? SymptomsReport { get; set; }

        public SnakebiteIncidentStatus Status { get; set; } = SnakebiteIncidentStatus.Pending;

        public DateTime CreatedAt { get; set; }

        public DateTime? AssignedAt { get; set; }

        public Guid? AssignedRescuerId { get; set; }

        public string? CancellationReason { get; set; }

        public int? SeverityLevel { get; set; } = 1;

        public DateTime? IncidentOccurredAt { get; set; }

        public SnakeSpeciesResponse? IdentifiedSnake { get; set; }

        public SnakeIdentificationContext? IdentificationContext { get; set; }

        public BriefMemberProfileResponse User { get; set; }

        public BriefRescuerProfileResponse? AssignedRescuer { get; set; }

        public int TotalRescueAttempts { get; set; }

        public int FailedAttemptsCount { get; set; }

        public int TotalDispatchRequests { get; set; }

        public int AcceptedDispatchCount { get; set; }

        public int DeclinedDispatchCount { get; set; }

        public int CancelledDispatchCount { get; set; }

        // Member-submitted incident media (snake identification verification).
        public List<SnakeAIDetectMediaResponse> Media { get; set; } = new List<SnakeAIDetectMediaResponse>();

        // Full rescue mission history with rescuer evidence media per mission.
        public List<AdminRescueMissionHistoryItemResponse> MissionHistory { get; set; } = new List<AdminRescueMissionHistoryItemResponse>();

        public List<AdminDispatchRequestHistoryItemResponse> DispatchRequests { get; set; } = new List<AdminDispatchRequestHistoryItemResponse>();
    }

    public class AdminRescueMissionHistoryItemResponse
    {
        public Guid MissionId { get; set; }

        public Guid RescuerId { get; set; }

        public string RescuerName { get; set; } = string.Empty;

        public string RescuerPhone { get; set; } = string.Empty;

        public RescueMissionStatus Status { get; set; }

        public decimal Price { get; set; }

        public decimal? ActualCost { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? ArrivedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? Notes { get; set; }

        public string? CancellationReason { get; set; }

        public List<ReportMediaResponse> Media { get; set; } = new List<ReportMediaResponse>();
    }

    public class AdminDispatchRequestHistoryItemResponse
    {
        public Guid RequestId { get; set; }

        public Guid RescuerId { get; set; }

        public string RescuerName { get; set; } = string.Empty;

        public string RescuerPhone { get; set; } = string.Empty;

        public Guid? OperatorId { get; set; }

        public string? OperatorName { get; set; }

        public RescueRequestStatus Status { get; set; }

        public DateTime DispatchedAt { get; set; }

        public DateTime? ResponseAt { get; set; }

        public string? DeclineReason { get; set; }
    }
}