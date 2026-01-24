using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class SpeciesAntivenom : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(SnakeSpecies))]
        public int SnakeSpeciesId { get; set; }

        [Required]
        [ForeignKey(nameof(Antivenom))]
        public int AntivenomId { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;


        // Navigation properties
        public SnakeSpecies SnakeSpecies { get; set; }
        public Antivenom Antivenom { get; set; }
    }
}