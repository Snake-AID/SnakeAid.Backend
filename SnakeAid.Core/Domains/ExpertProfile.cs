using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ExpertProfile : BaseEntity
    {
        [Key]
        [ForeignKey(nameof(Account))]
        public Guid AccountId { get; set; }

        [Required]
        public string Biography { get; set; }

        public bool IsOnline { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ConsultationFee { get; set; }

        [Range(0.0, 5.0)]
        public float Rating { get; set; }

        [Range(0, int.MaxValue)]
        public int RatingCount { get; set; }

        // Navigation properties
        public Account Account { get; set; }
        public List<Specialization> Specializations { get; set; } = new List<Specialization>();
    }
}