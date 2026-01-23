using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class Antivenom : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Manufacturer { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public ICollection<SpeciesAntivenom> SpeciesAntivenoms { get; set; } = new List<SpeciesAntivenom>();
    }
}