using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class SymptomConfig : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int SeverityScore { get; set; }
        public bool IsCharacteristic { get; set; }
        public bool IsActive { get; set; }

        public int VenomTypeId { get; set; }
        public VenomType VenomType { get; set; }
    }
}