using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ReputationTransaction : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey(nameof(RawEvent))]
        public Guid RawEventId { get; set; }  // Link tới raw event đã được process

        [Required]
        [ForeignKey(nameof(ReputationRule))]
        public int ReputationRuleId { get; set; }

        [Required]
        public int PointsChanged { get; set; }  // Số điểm thay đổi (+/-)

        [Required]
        public int PreviousPoints { get; set; }  // Điểm trước khi thay đổi

        [Required]
        public int NewPoints { get; set; }  // Điểm sau khi thay đổi

        public string? Notes { get; set; }  // Ghi chú thêm


        // Navigation properties
        public Account User { get; set; }
        public ReputationRawEvent RawEvent { get; set; }
        public ReputationRule ReputationRule { get; set; }
    }
}