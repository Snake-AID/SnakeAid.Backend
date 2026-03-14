using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.MemberProfile;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using SnakeAid.Core.Responses.SnakeDetection;
using SnakeAid.Core.Responses.UserFeedback;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakeCatchingRequest
{
    public class DetailSnakeCatchingRequestResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Address { get; set; }

        public Point LocationCoordinates { get; set; }

        /// <summary>
        /// Longitude for easy client access (extracted from LocationCoordinates)
        /// </summary>
        public double Lng { get; set; }

        /// <summary>
        /// Latitude for easy client access (extracted from LocationCoordinates)
        /// </summary>
        public double Lat { get; set; }

        public string AdditionalDetails { get; set; }

        public RequestStatus Status { get; set; }

        public uint Version { get; set; }

        public Guid? HandlingOperatorId { get; set; }

        public string? OperatorNotes { get; set; }

        public RequestPriority Priority { get; set; }

        public DateTime RequestDate { get; set; }

        public DateTime? PreferredTime { get; set; }

        public DateTime? DispatchedAt { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        public DateTime? AssignedAt { get; set; }

        public Guid? AssignedRescuerId { get; set; }

        public double? DistanceKm { get; set; }

        public decimal? EstimatedPrice { get; set; }

        public string? CancellationReason { get; set; }

        public string? Notes { get; set; }


        // Navigation properties
        public BriefMemberProfileResponse User { get; set; }
        public UserInfo? HandlingOperator { get; set; }
        public BriefRescuerProfileResponse? AssignedRescuer { get; set; }
        public List<SnakeCatchingMissionDetailResponse> Missions { get; set; } = new List<SnakeCatchingMissionDetailResponse>();
        public List<ReportMediaResponse> Media { get; set; } = new List<ReportMediaResponse>();
        public List<CatchingRequestDetailResponse> Details { get; set; } = new List<CatchingRequestDetailResponse>();
        public List<UserFeedbackResponse> Feedbacks { get; set; } = new List<UserFeedbackResponse>();
        public List<SnakeDetectionResponse> AIResults { get; set; } = new List<SnakeDetectionResponse>();
    }
}
