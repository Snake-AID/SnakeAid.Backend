using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class Specialization : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Name { get; set; }

        // Navigation properties
        public ICollection<ExpertSpecialization> ExpertSpecializations { get; set; } = new List<ExpertSpecialization>();
    }
}