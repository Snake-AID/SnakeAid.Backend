using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.SnakeCatchingRequest;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakeCatchingMission
{
    public class CreateSnakeCatchingMissionResponse
    {
        public Guid Id { get; set; }

        
        public Guid RescuerId { get; set; }

        
        public Guid SnakeCatchingRequestId { get; set; }

        public CatchingMissionStatus Status { get; set; } = CatchingMissionStatus.Preparing;

        
        public decimal Price { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? ArrivedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? Notes { get; set; }

        public string? CancellationReason { get; set; }

        public decimal? EstimatedCost { get; set; }

        public decimal? ActualCost { get; set; }

        // Navigation properties
        public BriefRescuerProfileResponse Rescuer { get; set; }
        public CreateSnakeCatchingRequestResponse SnakeCatchingRequest { get; set; }
    }
}
