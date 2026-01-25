using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnakeAid.Core.Domains
{
    public class MemberProfile : BaseEntity
    {
        [Key]
        [ForeignKey(nameof(Account))]
        public Guid AccountId { get; set; }

        [Range(0.0, 5.0)]
        public float Rating { get; set; }

        [Range(0, int.MaxValue)]
        public int RatingCount { get; set; }

        [Column(TypeName = "jsonb")]
        public List<string> EmergencyContacts { get; set; } = new List<string>();

        public bool HasUnderlyingDisease { get; set; }


        // Navigation properties
        public Account Account { get; set; }
        public ICollection<SnakebiteIncident> SnakebiteIncidents { get; set; } = new List<SnakebiteIncident>();
        public ICollection<SnakeCatchingRequest> SnakeCatchingRequests { get; set; } = new List<SnakeCatchingRequest>();
    }
}