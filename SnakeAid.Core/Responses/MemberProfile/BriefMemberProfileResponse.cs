using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.MemberProfile
{
    public class BriefMemberProfileResponse
    {

        public Guid AccountId { get; set; }

        [Range(0.0, 5.0)]
        public float Rating { get; set; }

        [Range(0, int.MaxValue)]
        public int RatingCount { get; set; }

        [Column(TypeName = "jsonb")]
        public List<string> EmergencyContacts { get; set; } = new List<string>();

        public bool HasUnderlyingDisease { get; set; }


        // Navigation properties
        public UserInfo Account { get; set; }
    }
}
