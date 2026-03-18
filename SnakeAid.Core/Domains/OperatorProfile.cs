using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnakeAid.Core.Domains
{
    public class OperatorProfile : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Account))]
        public Guid AccountId { get; set; }

        [Required]
        public bool IsOnDuty { get; set; } = false;

        [Required]
        [Range(0, int.MaxValue)]
        public int CurrentCaseCount { get; set; } = 0;

        [Required]
        [Range(1, 20)]
        public int MaxConcurrentCases { get; set; } = 3;

        [Required]
        public bool IsAcceptingNew { get; set; } = true;

        public Account Account { get; set; }
    }
}
