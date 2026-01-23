using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class CatchingMissionDetail : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid SnakeCatchingMissionId { get; set; }
        public int SnakeSpeciesId { get; set; }
        public int Quantity { get; set; }

        public SnakeCatchingMission SnakeCatchingMission { get; set; }
    }
}