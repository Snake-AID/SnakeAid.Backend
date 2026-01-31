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
        [MaxLength(2000)]
        public string Biography { get; set; }

        [Required]
        public bool IsOnline { get; set; } = false;

        [Range(0, 999999.99)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal ConsultationFee { get; set; }

        [Range(0.0, 5.0)]
        [Column(TypeName = "numeric(3,2)")]
        public decimal Rating { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int RatingCount { get; set; } = 0;


        // Navigation properties
        public Account Account { get; set; }
        public ICollection<ExpertSpecialization> Specializations { get; set; } = new List<ExpertSpecialization>();
    }
}