using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.SnakebiteIncident
{
    public class CancelIncidentRequest
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}