using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.MemberProfile;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakeCatchingRequest
{
    public class CreateSnakeCatchingRequestResponse
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

        public RequestPriority Priority { get; set; } 

        public DateTime RequestDate { get; set; } 

        public DateTime? PreferredTime { get; set; }

        public DateTime? AssignedAt { get; set; }

        public Guid? AssignedRescuerId { get; set; }

        public decimal? EstimatedPrice { get; set; }

        public string? CancellationReason { get; set; }

        public string? Notes { get; set; }


        // Navigation properties
        public BriefMemberProfileRespone User { get; set; }
        public BriefRescuerProfileResponse? AssignedRescuer { get; set; }
        public CreateSnakeCatchingMissionResponse? Mission { get; set; }
        public List<ReportMediaResponse> Media { get; set; } = new List<ReportMediaResponse>();
        public List<CatchingRequestDetailResponse> Details { get; set; } = new List<CatchingRequestDetailResponse>();
    }
}
