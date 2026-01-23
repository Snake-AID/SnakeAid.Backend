using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class RescuerProfile : BaseEntity
    {
        [Key]
        [ForeignKey(nameof(Account))]
        public Guid AccountId { get; set; }
        public bool IsOnline { get; set; }
        [Range(0.0, 5.0)]
        public float Rating { get; set; }
        [Range(0, int.MaxValue)]
        public int RatingCount { get; set; }
        [Required]
        public RescuerType Type { get; set; } = RescuerType.Emergency;

        // Navigation property
        public Account Account { get; set; }
    }

    public enum RescuerType
    {
        Emergency = 0,
        Catching = 1,
        Both = 2,
    }
}