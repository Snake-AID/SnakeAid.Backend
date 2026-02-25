using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.RescueMission
{
    public class CancelMissionRequest
    {
        [MaxLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters")]
        public string? CancellationReason { get; set; }
    }
}