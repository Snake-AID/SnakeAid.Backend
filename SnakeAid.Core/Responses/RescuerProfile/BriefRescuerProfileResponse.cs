using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.RescuerProfile
{
    public class BriefRescuerProfileResponse
    {
        
        public Guid AccountId { get; set; }

        [Required]
        public bool IsOnline { get; set; } = false;

        [Range(0.0, 5.0)]
        [Column(TypeName = "numeric(3,2)")]
        public decimal Rating { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int RatingCount { get; set; } = 0;

        public RescuerType Type { get; set; } = RescuerType.Emergency;

        // PostGIS Location tracking
        [Column(TypeName = "geometry(Point, 4326)")]
        public Point? LastLocation { get; set; }

        public DateTime? LastLocationUpdate { get; set; }

        // Statistics
        public int TotalMissions { get; set; } = 0;

        public int CompletedMissions { get; set; } = 0;


        // Navigation properties
        public UserInfo Account { get; set; }
    }
}
