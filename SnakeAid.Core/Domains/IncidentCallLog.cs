using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnakeAid.Core.Domains
{
    public class IncidentCallLog : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Incident))]
        public Guid IncidentId { get; set; }

        [Required]
        [ForeignKey(nameof(Operator))]
        public Guid OperatorId { get; set; }

        [Required]
        public DateTime CalledAt { get; set; } = DateTime.UtcNow;

        [Range(0, int.MaxValue)]
        public int? Duration { get; set; }

        [Required]
        public IncidentCallOutcome Outcome { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public SnakebiteIncident Incident { get; set; }
        public Account Operator { get; set; }
    }

    public enum IncidentCallOutcome
    {
        Confirmed = 0,
        FalseAlarm = 1,
        NoAnswer = 2,
        Cancelled = 3
    }
}
