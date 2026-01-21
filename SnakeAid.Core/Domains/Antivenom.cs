using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class Antivenom : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string Description { get; set; }

        public ICollection<SpeciesAntivenom> SpeciesAntivenoms { get; set; } = new List<SpeciesAntivenom>();
    }
}