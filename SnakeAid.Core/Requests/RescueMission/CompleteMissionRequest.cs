using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.RescueMission
{
    public class CompleteMissionRequest
    {
        [Required(ErrorMessage = "At least one evidence photo is required")]
        [MinLength(1, ErrorMessage = "At least one evidence photo is required")]
        public List<Guid> EvidenceMediaIds { get; set; } = new();

        [MaxLength(1000, ErrorMessage = "Completion notes cannot exceed 1000 characters")]
        public string? CompletionNotes { get; set; }
    }
}