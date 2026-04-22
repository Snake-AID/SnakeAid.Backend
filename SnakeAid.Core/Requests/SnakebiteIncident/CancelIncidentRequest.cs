using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.SnakebiteIncident
{
    public class CancelIncidentRequest
    {
        [Required(ErrorMessage = "Cancel Reason is required.")]
        [MaxLength(1000, ErrorMessage = "Cancel Reason cannot exceed 1000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}