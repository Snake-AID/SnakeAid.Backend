using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class FilterSnakeMapping
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(FilterOption))]
        public int FilterOptionId { get; set; }

        [Required]
        [ForeignKey(nameof(SnakeSpecies))]
        public int SnakeSpeciesId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsActive { get; set; } = true;


        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public FilterOption FilterOption { get; set; }
        public SnakeSpecies SnakeSpecies { get; set; }
    }
}