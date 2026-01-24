using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnakeAid.Core.Domains
{
    public class ExpertSpecialization : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Expert))]
        public Guid ExpertId { get; set; }

        [Required]
        [ForeignKey(nameof(Specialization))]
        public int SpecializationId { get; set; }


        // Navigation properties
        public ExpertProfile Expert { get; set; }
        public Specialization Specialization { get; set; }
    }
}