using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class CatchingRequestDetail : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid SnakeCatchingRequestId { get; set; }
        public int SnakeSpeciesId { get; set; }
        public int Quantity { get; set; }
    }
}