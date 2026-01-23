namespace SnakeAid.Core.Domains
{
    public class ConsultationPingRequest
    {
        public Guid Id { get; set; }
        public Guid RescuerId { get; set; }
        public Guid ExpertId { get; set; }
        public ConsultationPingStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public Guid? RescueMissionId { get; set; }  // Link back to mission
        public Guid? ConsultationId { get; set; }   // Created consultation if accepted

        // Navigation properties
        public RescuerProfile Rescuer { get; set; }
        public ExpertProfile Expert { get; set; }
        public RescueMission RescueMission { get; set; }
        public Consultation? Consultation { get; set; }

    }

    public enum ConsultationPingStatus
    {
        PendingExpertResponse = 0,
        AcceptedByExpert = 1,
        DeclinedByExpert = 2,
        RescuerCancelled = 3,
        Expired = 4
    }
}