using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class SnakeCatchingMission : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid RescuerId { get; set; }
        public Guid SnakeCatchingRequestId { get; set; }
        public MissionStatus Status { get; set; } = MissionStatus.Pending;

        public SnakeCatchingRequest SnakeCatchingRequest { get; set; }
    }

    public enum MissionStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
}