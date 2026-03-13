using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
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

        public DateTime? IncidentOccurredAt { get; set; }  // Khi nào bị cắn
    }

    public class GeoPointResponse
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

}
