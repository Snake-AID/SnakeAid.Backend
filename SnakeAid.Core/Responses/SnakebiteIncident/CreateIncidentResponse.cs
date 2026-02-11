using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.RescueRequestSession;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class CreateIncidentResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }  // FK to MemberProfile

        public GeoPointResponse LocationCoordinates { get; set; }

        public SnakebiteIncidentStatus Status { get; set; } = SnakebiteIncidentStatus.Pending;

        // Session ping info
        public int CurrentSessionNumber { get; set; } = 0;   // Track session hiện tại

        public int CurrentRadiusKm { get; set; } = 5;        // Radius hiện tại

        public DateTime? IncidentOccurredAt { get; set; }  // Khi nào bị cắn

        // Session details (populated when StartRescueAsync is called)
        public Guid? SessionId { get; set; }
        public int SessionNumber { get; set; }
        public int RadiusKm { get; set; }
        public int RescuersPinged { get; set; }

        public List<CreateRescueRequestSessionResponse> Sessions { get; set; } = new List<CreateRescueRequestSessionResponse>();
    }

    public class GeoPointResponse
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

}
