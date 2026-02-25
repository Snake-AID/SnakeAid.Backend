using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.RescueMission
{
    public class UpdateRescueMissionStatusRequest
    {
        [Required(ErrorMessage = "Status is required")]
        public RescueMissionStatus Status { get; set; }

        public string? Notes { get; set; }

        [MaxLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters")]
        public string? CancellationReason { get; set; }

        // public decimal? ActualCost { get; set; }

        // Required when completing mission
        public List<Guid>? VerificationImageIds { get; set; }
    }
}
