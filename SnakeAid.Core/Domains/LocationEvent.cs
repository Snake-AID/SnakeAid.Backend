using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class LocationEvent
    {
        public long Id { get; set; }
        public Guid SessionId { get; set; }
        public SessionType SessionType { get; set; }

        public Guid AccountId { get; set; }
        public TrackingRole Role { get; set; }

        // Dùng Point thay vì double Lat/Lng để đồng bộ PostGIS
        public Point Location { get; set; }

        public float Accuracy { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public float? Speed { get; set; }
        public float? Heading { get; set; }
    }

    public enum TrackingRole
    {
        Member = 0,
        Rescuer = 1
    }
}