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
        public bool IsAvailable { get; set; } = false;
        public string PhoneNumber { get; set; } = string.Empty;

        public decimal Rating { get; set; } = 0;
        public int RatingCount { get; set; } = 0;

        public RescuerType Type { get; set; } = RescuerType.Emergency;

        public DateTime? LastLocationUpdate { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Statistics
        public int TotalMissions { get; set; } = 0;

        public int CompletedMissions { get; set; } = 0;


        // Navigation properties
        public UserInfo Account { get; set; }
    }
}
