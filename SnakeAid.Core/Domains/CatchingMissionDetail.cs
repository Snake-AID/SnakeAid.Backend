using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class CatchingMissionDetail : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(SnakeCatchingMission))]
        public Guid SnakeCatchingMissionId { get; set; }

        [Required]
        [ForeignKey(nameof(SnakeSpecies))]
        public int SnakeSpeciesId { get; set; }

        [Required]
        public int Quantity { get; set; }


        public SnakeCatchingMission SnakeCatchingMission { get; set; }
        public SnakeSpecies SnakeSpecies { get; set; }
    }
}