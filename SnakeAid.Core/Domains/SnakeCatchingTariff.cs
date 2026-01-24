using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class SnakeCatchingTariff : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(SnakeSpecies))]
        public int SnakeSpeciesId { get; set; }

        [Required]
        public SizeCategory SizeCategory { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal BasePrice { get; set; }

        [Required]
        public string Currency { get; set; } = "VND";

        [Required]
        public bool IsActive { get; set; } = true;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        public SnakeSpecies SnakeSpecies { get; set; }
    }

    public enum SizeCategory
    {
        Small = 0,      // < 1m
        Medium = 1,     // 1-2m  
        Large = 2,      // 2-3m
        ExtraLarge = 3  // > 3m
    }
}