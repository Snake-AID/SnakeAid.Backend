using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.RescueMission;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class ListSnakebiteIncidentResponse
    {
        public Guid Id { get; set; }

        public GeoPointResponse LocationCoordinates { get; set; }

        public string Address { get; set; }

        public List<ReportSymptom>? SymptomsReport { get; set; }

        public SnakebiteIncidentStatus Status { get; set; } = SnakebiteIncidentStatus.Pending;

        public int? SeverityLevel { get; set; } = 1;

        public DateTime? IncidentOccurredAt { get; set; }

        public CreateRescueMissionResponse? ActiveMission { get; set; }
    }
}