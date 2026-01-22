using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ReputationRawEvent
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string EventType { get; set; }
        public bool IsProcessed { get; set; } = false;
        public int ReputationRuleId { get; set; }

        public ReputationRule ReputationRule { get; set; }
    }
}