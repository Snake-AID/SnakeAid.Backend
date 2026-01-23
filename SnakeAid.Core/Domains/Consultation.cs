using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class Consultation : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Caller))]
        public Guid CallerId { get; set; }

        [Required]
        [ForeignKey(nameof(Callee))]
        public Guid CalleeId { get; set; }

        [Required]
        public string RoomId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Required]
        public ConsultationStatus Status { get; set; }

        [Required]
        public ConsultationType Type { get; set; }


        public Account Caller { get; set; }
        public Account Callee { get; set; }

    }

    public enum ConsultationStatus
    {
        Scheduled = 0,
        Ongoing = 1,
        Completed = 2,
        Cancelled = 3,
        UserAbsent = 4,
        ExpertAbsent = 5,
        AllAbsent = 6
    }

    public enum ConsultationType
    {
        Emergency = 0,
        Scheduled = 1
    }
}