using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class SpeciesVenom
    {
        [Required]
        [ForeignKey(nameof(SnakeSpecies))]
        public int SnakeSpeciesId { get; set; }

        [Required]
        [ForeignKey(nameof(VenomType))]
        public int VenomTypeId { get; set; }


        public VenomType VenomType { get; set; }
        public SnakeSpecies SnakeSpecies { get; set; }
    }
}