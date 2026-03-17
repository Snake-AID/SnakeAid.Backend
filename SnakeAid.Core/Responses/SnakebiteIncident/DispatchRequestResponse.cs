using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class DispatchRequestResponse
    {
        public Guid RequestId { get; set; }
        public Guid RescuerId { get; set; }
        public string RescuerName { get; set; } = string.Empty;
        public string RescuerPhone { get; set; } = string.Empty;
        public RescueRequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResponseAt { get; set; }
        public string? DeclineReason { get; set; }
    }
}
