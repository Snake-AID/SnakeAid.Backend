using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class SnakeCatchingTariff : BaseEntity
    {
        public Guid Id { get; set; }
        public int SnakeSpeciesId { get; set; }  // FK to SnakeSpecies
        public SizeCategory SizeCategory { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }

        // Navigation properties
        public SnakeSpecies SnakeSpecies { get; set; }
    }

    public enum SizeCategory
    {
        Small = 0,    // < 1m
        Medium = 1,   // 1-2m  
        Large = 2,    // 2-3m
        ExtraLarge = 3 // > 3m
    }
}