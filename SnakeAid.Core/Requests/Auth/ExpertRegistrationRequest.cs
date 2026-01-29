using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.Auth
{
    public class ExpertRegistrationRequest
    {
        [MaxLength(2000)]
        public string Biography { get; set; }
    }
}
