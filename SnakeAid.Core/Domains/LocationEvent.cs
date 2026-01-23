using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class LocationEvent
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public Guid SessionId { get; set; }

        [Required]
        public SessionType SessionType { get; set; }

        [Required]
        public Guid AccountId { get; set; }

        [Required]
        public TrackingRole Role { get; set; }

        [Required]
        public Point Location { get; set; }

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