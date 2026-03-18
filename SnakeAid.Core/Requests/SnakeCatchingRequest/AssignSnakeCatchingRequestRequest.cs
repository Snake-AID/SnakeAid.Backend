using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.SnakeCatchingRequest
{
    public class AssignSnakeCatchingRequestRequest
    {
        public Guid rescuerId { get; set; }
    }
}
