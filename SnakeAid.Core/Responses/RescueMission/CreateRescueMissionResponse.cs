using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.TreatmentFacility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.RescueMission
{
    public class CreateRescueMissionResponse
    {
        public Guid Id { get; set; }


        public Guid IncidentId { get; set; }  // FK to SnakebiteIncident (1-1)


        public Guid RescuerId { get; set; }   // FK to RescuerProfile

        public RescueMissionStatus Status { get; set; } = RescueMissionStatus.Preparing;

        public decimal Price { get; set; }

        public decimal? CostFromCenter { get; set; }

        public decimal? DistanceFromCenterKm { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? ArrivedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }

        public decimal? ActualCost { get; set; }

        public bool RequiresHospitalization { get; set; } = false;

        public int? HospitalId { get; set; }

        public HospitalTransferResponse? Hospital { get; set; }

        public BriefRescuerProfileResponse Rescuer { get; set; }
    }
}
