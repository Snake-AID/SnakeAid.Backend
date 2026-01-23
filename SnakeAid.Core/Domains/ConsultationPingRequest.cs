using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ConsultationPingRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Rescuer))]
        public Guid RescuerId { get; set; }

        [Required]
        [ForeignKey(nameof(Expert))]
        public Guid ExpertId { get; set; }

        [Required]
        public ConsultationPingStatus Status { get; set; } = ConsultationPingStatus.PendingExpertResponse;

        [Required]
        public DateTime RequestedAt { get; set; }

        public DateTime? RespondedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [ForeignKey(nameof(RescueMission))]
        public Guid? RescueMissionId { get; set; }  // Link back to mission

        [ForeignKey(nameof(Consultation))]
        public Guid? ConsultationId { get; set; }   // Created consultation if accepted


        // Navigation properties
        public Account Rescuer { get; set; }
        public Account Expert { get; set; }
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