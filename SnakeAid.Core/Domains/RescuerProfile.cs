using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class RescuerProfile : BaseEntity
    {
        [Key]
        [ForeignKey(nameof(Account))]
        public Guid AccountId { get; set; }

        [Required]
        public bool IsOnline { get; set; } = false;

        [Required]
        [Range(0.0, 5.0)]
        [Column(TypeName = "numeric(3,2)")]
        public decimal Rating { get; set; } = 0;

        [Required]
        [Range(0, int.MaxValue)]
        public int RatingCount { get; set; } = 0;

        [Required]
        public RescuerType Type { get; set; } = RescuerType.Emergency;

        // PostGIS Location tracking
        [Column(TypeName = "geometry(Point, 4326)")]
        public Point? LastLocation { get; set; }

        public DateTime? LastLocationUpdate { get; set; }

        // Statistics
        [Required]
        public int TotalMissions { get; set; } = 0;

        [Required]
        public int CompletedMissions { get; set; } = 0;


        // Navigation properties
        public Account Account { get; set; }
        public ICollection<RescueMission> Missions { get; set; } = new List<RescueMission>();
        public ICollection<SnakeCatchingMission> CatchingMissions { get; set; } = new List<SnakeCatchingMission>();
        public ICollection<RescuerRequest> RescuerRequests { get; set; } = new List<RescuerRequest>();
    }

    public enum RescuerType
    {
        Emergency = 0,
        Catching = 1,
        Both = 2,
    }
}