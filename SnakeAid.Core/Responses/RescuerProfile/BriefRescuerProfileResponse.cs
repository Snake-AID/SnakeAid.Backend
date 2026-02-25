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

        public bool IsOnline { get; set; } = false;

        public decimal Rating { get; set; } = 0;
        public int RatingCount { get; set; } = 0;

        public RescuerType Type { get; set; } = RescuerType.Emergency;

        public Point? LastLocation { get; set; }

        public DateTime? LastLocationUpdate { get; set; }

        // Statistics
        public int TotalMissions { get; set; } = 0;

        public int CompletedMissions { get; set; } = 0;


        // Navigation properties
        public UserInfo Account { get; set; }
    }
}
